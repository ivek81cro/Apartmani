(() => {
    const button = document.querySelector('.menu-toggle');
    const menu = document.querySelector('#main-menu');

    if (!button || !menu) return;

    button.addEventListener('click', () => {
        const open = button.getAttribute('aria-expanded') === 'true';
        button.setAttribute('aria-expanded', String(!open));
        menu.classList.toggle('is-open', !open);
    });
})();

(() => {
    const dialog = document.querySelector('#gallery-dialog');
    const items = Array.from(document.querySelectorAll('[data-gallery-item]'));
    if (!dialog || !items.length || typeof dialog.showModal !== 'function') return;
    const image = dialog.querySelector('.gallery-full-image');
    const counter = dialog.querySelector('.gallery-counter');
    const thumbnails = Array.from(dialog.querySelectorAll('[data-gallery-thumbnail]'));
    const thumbnailRail = dialog.querySelector('.gallery-thumbnails');
    let current = 0;
    let opener = null;
    function show(index) {
        current = (index + items.length) % items.length;
        image.classList.remove('gallery-image-enter');
        image.src = items[current].href;
        image.alt = items[current].querySelector('img').alt;
        counter.textContent = `${current + 1} / ${items.length}`;
        thumbnails.forEach((thumbnail, index) => {
            const selected = index === current;
            thumbnail.classList.toggle('is-active', selected);
            if (selected) thumbnail.setAttribute('aria-current', 'true');
            else thumbnail.removeAttribute('aria-current');
        });
        const active = thumbnails[current];
        if (active) {
            const left = active.offsetLeft - thumbnailRail.offsetLeft;
            const right = left + active.offsetWidth;
            if (left < thumbnailRail.scrollLeft) thumbnailRail.scrollLeft = left;
            else if (right > thumbnailRail.scrollLeft + thumbnailRail.clientWidth) {
                thumbnailRail.scrollLeft = right - thumbnailRail.clientWidth;
            }
        }
        void image.offsetWidth;
        image.classList.add('gallery-image-enter');
    }
    items.forEach((item, index) => item.addEventListener('click', event => {
        event.preventDefault();
        opener = item;
        dialog.showModal();
        document.body.classList.add('gallery-open');
        show(index);
    }));
    thumbnails.forEach((thumbnail, index) => thumbnail.addEventListener('click', () => show(index)));
    dialog.querySelectorAll('.gallery-close, .gallery-close-icon')
        .forEach(button => button.addEventListener('click', () => dialog.close()));
    dialog.querySelector('.gallery-prev').addEventListener('click', () => show(current - 1));
    dialog.querySelector('.gallery-next').addEventListener('click', () => show(current + 1));
    dialog.addEventListener('keydown', event => {
        if (event.key === 'ArrowLeft') {
            event.preventDefault();
            show(current - 1);
        }
        if (event.key === 'ArrowRight') {
            event.preventDefault();
            show(current + 1);
        }
    });
    dialog.addEventListener('click', event => {
        if (event.target === dialog) dialog.close();
    });
    dialog.addEventListener('close', () => {
        document.body.classList.remove('gallery-open');
        if (opener) opener.focus();
    });
})();
