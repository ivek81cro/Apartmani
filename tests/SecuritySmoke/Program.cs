using Apartmani.Data;
using Apartmani.Utilities;
using Apartmani.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Localization;
using Apartmani.Services;
using Apartmani.Pages;
using Apartmani;
using System.Net.Mail;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Resources;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ImageCreate = Apartmani.Pages.Images.CreateModel;
using ImageDelete = Apartmani.Pages.Images.DeleteModel;
using CategoryEdit = Apartmani.Pages.ImageCategories.EditModel;
using CategoryDelete = Apartmani.Pages.ImageCategories.DeleteModel;

var root=Path.Combine(Path.GetTempPath(),"apartmani-security-test-"+Guid.NewGuid());
Directory.CreateDirectory(Path.Combine(root,"img"));
var env=new TestEnvironment { WebRootPath=root };
using var sql=new SqliteConnection("Data Source=:memory:"); sql.Open();
var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(sql).Options;
using var db=new ApplicationDbContext(options); db.Database.EnsureCreated();
var cat=new ImageCategory {Name="Custom"}; var gallery=new ImageCategory {Name="Gallery"}; db.ImageCategory.AddRange(cat,gallery); db.SaveChanges();
byte[] CreateImage(SkiaSharp.SKEncodedImageFormat format)
{
    using var bitmap = new SkiaSharp.SKBitmap(8, 8);
    using var canvas = new SkiaSharp.SKCanvas(bitmap);
    canvas.Clear(SkiaSharp.SKColors.CornflowerBlue);
    using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
    using var encoded = image.Encode(format, 90);
    return encoded.ToArray();
}
var jpeg=CreateImage(SkiaSharp.SKEncodedImageFormat.Jpeg);
async Task<(IActionResult,ImageCreate)> Upload(byte[] bytes,string name) {
 var data=new MemoryStream(bytes); var files=new FormFileCollection {new FormFile(data,0,bytes.Length,"files",name)};
 var ctx=new DefaultHttpContext(); ctx.Request.Form=new FormCollection(new Dictionary<string,Microsoft.Extensions.Primitives.StringValues>(),files);
 var page=new ImageCreate(db,env){PageContext=new PageContext{HttpContext=ctx},Image=new Image{CategoryId=cat.Id}};
 page.PageContext.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
 return (await page.OnPostAsync(),page);
}
void Check(bool ok,string description) { if(!ok) throw new Exception(description); Console.WriteLine("PASS "+description); }
var good=await Upload(jpeg,"misleading.exe"); Check(good.Item1 is RedirectToPageResult,"valid JPEG accepted using detected format");
var image=db.Image.Single(); var path=ImageStorage.ResolveImagePath(root,image.Path); Check(image.Path.EndsWith(".jpg")&&File.Exists(path),"safe generated path and file exist");
var png=CreateImage(SkiaSharp.SKEncodedImageFormat.Png);
var goodPng=await Upload(png,"test.png"); Check(goodPng.Item1 is RedirectToPageResult && db.Image.Count()==2,"valid PNG accepted");
var pngImage=db.Image.Single(item => item.Id != image.Id); var pngPath=ImageStorage.ResolveImagePath(root,pngImage.Path);
var imageEditor=new Apartmani.Pages.Images.EditModel(db){Image=new Image{Id=pngImage.Id,CategoryId=gallery.Id,Path="img/../../outside.txt"}};
Check(await imageEditor.OnPostAsync() is RedirectToPageResult && db.Image.Find(pngImage.Id).Path==pngImage.Path,"editing cannot overwrite stored path");
var corrupt=await Upload(new byte[]{255,216,255,1,2,255,217},"fake.jpg"); Check(corrupt.Item1 is PageResult && db.Image.Count()==2,"malformed JPEG rejected");
Check(ImageStorage.ResolveImagePath(root,"img/../../outside.txt")==null,"path traversal rejected");
var edit=new CategoryEdit(db){ImageCategory=new ImageCategory{Id=cat.Id,Name="Renamed"}}; var renamed=await edit.OnPostAsync(); Check(renamed is RedirectToPageResult && File.Exists(path) && db.Image.Find(image.Id).Path==image.Path,"renaming category preserves image file and path");
var catDelete=new CategoryDelete(db); Check(await catDelete.OnPostAsync(cat.Id) is PageResult,"non-empty category cannot be deleted"); Check(await catDelete.OnPostAsync(gallery.Id) is PageResult,"built-in category cannot be deleted");
var delete=new ImageDelete(db,env,NullLogger<ImageDelete>.Instance); Check(await delete.OnPostAsync(image.Id) is RedirectToPageResult && !File.Exists(path) && db.Image.Count()==1,"image and record deleted");
var sender = new TestContactSender();
var localizer = new TestLocalizer();
var contact = new ContactModel(sender, localizer, NullLogger<ContactModel>.Instance)
{
    Name = "Test guest", Email = "guest@example.invalid", Message = "Hello", Website = "bot-filled"
};
Check(await contact.OnPostAsync() is RedirectToPageResult && sender.Calls == 0, "honeypot does not send email");
contact.Website = null;
Check(await contact.OnPostAsync() is RedirectToPageResult && sender.Calls == 1, "valid enquiry is sent");
sender.Fail = true;
var errorPage = await contact.OnPostAsync();
Check(errorPage is PageResult && !contact.ModelState.IsValid && contact.Message == "Hello", "SMTP failure preserves enquiry and shows error");

using (var smtpListener = new TcpListener(IPAddress.Loopback, 0))
{
    smtpListener.Start();
    var smtpPort = ((IPEndPoint)smtpListener.LocalEndpoint).Port;
    var capturedMail = CaptureSmtpMessage(smtpListener);
    var realSender = new EmailSender("127.0.0.1", smtpPort, false, "sender@example.invalid", "");
    await realSender.SendContactAsync("owner@example.invalid", "Guest", "guest@example.invalid", "Test");
    var rawMail = await capturedMail.WaitAsync(TimeSpan.FromSeconds(10));
    Check(rawMail.Contains("Reply-To: guest@example.invalid", StringComparison.OrdinalIgnoreCase), "SMTP message sets guest reply-to");
}

static async Task<string> CaptureSmtpMessage(TcpListener listener)
{
    using var client = await listener.AcceptTcpClientAsync();
    using var stream = client.GetStream();
    using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
    using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
    await writer.WriteLineAsync("220 localhost ESMTP");
    var message = new StringBuilder();
    var inData = false;
    while (await reader.ReadLineAsync() is { } line)
    {
        if (inData)
        {
            if (line == ".") { await writer.WriteLineAsync("250 accepted"); inData = false; }
            else message.AppendLine(line);
        }
        else if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync("250-localhost");
            await writer.WriteLineAsync("250 SIZE 10485760");
        }
        else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
        {
            inData = true;
            await writer.WriteLineAsync("354 End with .");
        }
        else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync("221 Bye");
            break;
        }
        else await writer.WriteLineAsync("250 OK");
    }
    return message.ToString();
}

var resources = new ResourceManager("Apartmani.Resources.SharedResource", typeof(SharedResource).Assembly);
Check(resources.GetString("Kontakt", CultureInfo.GetCultureInfo("hr")) == "Kontakt", "Croatian resources available");
Check(resources.GetString("Kontakt", CultureInfo.GetCultureInfo("en")) == "Contact", "English resources available");
Check(resources.GetString("Kontakt", CultureInfo.GetCultureInfo("de")) == "Kontakt", "German resources available");
Check(resources.GetString("Poruka je poslana.", CultureInfo.GetCultureInfo("de")) == "Ihre Nachricht wurde gesendet.", "German success message translated");

var services = new ServiceCollection();
services.AddLogging();
new Startup(new ConfigurationBuilder().Build()).ConfigureServices(services);
using (var provider = services.BuildServiceProvider())
{
    var forwardedOptions = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
    var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask,
        provider.GetRequiredService<ILoggerFactory>(), forwardedOptions);
    var trusted = new DefaultHttpContext();
    trusted.Connection.RemoteIpAddress = IPAddress.Loopback;
    trusted.Request.Scheme = "http";
    trusted.Request.Headers["X-Forwarded-For"] = "198.51.100.55, 203.0.113.42";
    trusted.Request.Headers["X-Forwarded-Proto"] = "https";
    await middleware.Invoke(trusted);
    Check(trusted.Connection.RemoteIpAddress.ToString() == "203.0.113.42" && trusted.Request.Scheme == "https", "only nearest trusted proxy supplies forwarded address and scheme");
    var untrusted = new DefaultHttpContext();
    untrusted.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.10");
    untrusted.Request.Scheme = "http";
    untrusted.Request.Headers["X-Forwarded-For"] = "203.0.113.5";
    untrusted.Request.Headers["X-Forwarded-Proto"] = "https";
    await middleware.Invoke(untrusted);
    Check(untrusted.Connection.RemoteIpAddress.ToString() == "198.51.100.10" && untrusted.Request.Scheme == "http", "untrusted proxy headers ignored");
}

class TestContactSender : IContactEmailSender
{
    public int Calls { get; private set; }
    public bool Fail { get; set; }
    public Task SendContactAsync(string recipient, string name, string email, string message)
    {
        Calls++;
        if (Fail) throw new SmtpException("Simulated SMTP failure");
        return Task.CompletedTask;
    }
}

class TestLocalizer : IStringLocalizer<SharedResource>
{
    public LocalizedString this[string name] => new LocalizedString(name, name);
    public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, string.Format(name, arguments));
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
}

class TestEnvironment : IWebHostEnvironment {
 public string WebRootPath {get;set;} public IFileProvider WebRootFileProvider {get;set;}=new NullFileProvider();
 public string ApplicationName {get;set;}="Test"; public IFileProvider ContentRootFileProvider {get;set;}=new NullFileProvider();
 public string ContentRootPath {get;set;}=Environment.CurrentDirectory; public string EnvironmentName {get;set;}="Development";
}



