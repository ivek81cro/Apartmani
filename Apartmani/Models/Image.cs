using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Apartmani.Models
{
    public class Image
    {
        [Key]
        public int Id { get; set; }
        public string Path { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual ImageCategory ImageCategory { get; set; }
    }
}
