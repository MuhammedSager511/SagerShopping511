(() => {
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const isNarrow = window.matchMedia('(max-width: 991.98px)').matches;
  const canAnimate = !reduceMotion && !isNarrow && 'IntersectionObserver' in window;

  // ── Site loader / progress ──
  const loader = document.getElementById('site-loader');
  const progress = document.getElementById('site-progress');
  let hideTimer = null;

  const hideLoader = () => {
    if (!loader) return;
    loader.classList.add('is-hidden');
    loader.setAttribute('aria-busy', 'false');
    if (progress) progress.classList.remove('is-on');
  };

  const showLoader = (soft) => {
    if (progress) progress.classList.add('is-on');
    if (soft || !loader) return;
    loader.classList.remove('is-hidden');
    loader.setAttribute('aria-busy', 'true');
  };

  window.SagerShop = window.SagerShop || {};
  window.SagerShop.showLoader = showLoader;
  window.SagerShop.hideLoader = hideLoader;

  // Hide initial splash quickly once DOM is ready
  const bootHide = () => {
    window.clearTimeout(hideTimer);
    hideTimer = window.setTimeout(hideLoader, reduceMotion ? 0 : 180);
  };
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootHide, { once: true });
  } else {
    bootHide();
  }
  window.addEventListener('load', () => window.setTimeout(hideLoader, 60), { once: true });
  // Safety: never leave splash forever
  window.setTimeout(hideLoader, 4000);

  const sameOrigin = (url) => {
    try {
      const u = new URL(url, window.location.href);
      return u.origin === window.location.origin;
    } catch {
      return false;
    }
  };

  // Soft progress on internal navigation
  document.addEventListener('click', (e) => {
    const a = e.target.closest && e.target.closest('a[href]');
    if (!a) return;
    if (a.hasAttribute('data-no-loader')) return;
    if (a.target === '_blank' || a.hasAttribute('download')) return;
    if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
    const href = a.getAttribute('href') || '';
    if (!href || href.startsWith('#') || href.startsWith('mailto:') || href.startsWith('tel:') || href.startsWith('javascript:')) return;
    if (!sameOrigin(href)) return;
    if (a.getAttribute('data-bs-toggle') || a.getAttribute('data-bs-dismiss')) return;
    showLoader(true);
  });

  // Full overlay on form posts (skip search GETs / marked forms)
  document.addEventListener('submit', (e) => {
    const form = e.target;
    if (!(form instanceof HTMLFormElement)) return;
    if (form.hasAttribute('data-no-loader')) return;
    if ((form.method || 'get').toLowerCase() === 'get') {
      showLoader(true);
      return;
    }
    showLoader(false);
    const btn = form.querySelector('button[type="submit"], input[type="submit"]');
    if (btn && !btn.disabled) {
      btn.classList.add('is-loading');
      btn.setAttribute('disabled', 'disabled');
    }
  });

  // Homepage staggered reveals (desktop only — mobile stays fully visible)
  const revealEls = document.querySelectorAll('[data-reveal]');
  if (revealEls.length) {
    if (canAnimate) {
      document.documentElement.classList.add('js-reveal');

      const revealIo = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add('is-in');
          revealIo.unobserve(entry.target);
        });
      }, { threshold: 0.12, rootMargin: '0px 0px -6% 0px' });

      revealEls.forEach((el) => revealIo.observe(el));

      document.querySelectorAll('[data-home-intro] [data-reveal]').forEach((el, i) => {
        window.setTimeout(() => el.classList.add('is-in'), 40 + i * 80);
      });
    } else {
      revealEls.forEach((el) => el.classList.add('is-in'));
    }
  }

  // Soft reveal for other pages (desktop)
  if (canAnimate) {
    document.documentElement.classList.add('js-reveal');
    const els = document.querySelectorAll('.global-card, .category-sidebar, .product-card');
    els.forEach((el) => {
      if (el.hasAttribute('data-reveal') || el.classList.contains('reveal-up')) return;
      if (el.closest('[data-home-intro], .home-section, .home-closing, .mobile-bottom-nav')) return;
      el.classList.add('reveal-on-scroll');
    });

    const io = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible');
          io.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });

    document.querySelectorAll('.reveal-on-scroll').forEach((el) => io.observe(el));
  } else {
    document.querySelectorAll('.reveal-on-scroll').forEach((el) => el.classList.add('is-visible'));
  }

  // Active nav link highlight by path
  const path = (window.location.pathname || '/').toLowerCase();
  document.querySelectorAll('.store-navbar .nav-link, .admin-navbar .nav-link, .side-menu-link').forEach((link) => {
    const href = (link.getAttribute('href') || '').toLowerCase();
    if (!href || href === '#') return;
    if (path === href || (href !== '/' && path.startsWith(href))) {
      link.classList.add('active');
    }
  });

  // Close side menu after tapping a link (mobile)
  const sideMenu = document.getElementById('storeSideMenu');
  if (sideMenu) {
    sideMenu.querySelectorAll('a.side-menu-link').forEach((link) => {
      link.addEventListener('click', () => {
        const inst = window.bootstrap && bootstrap.Offcanvas.getInstance(sideMenu);
        if (inst) inst.hide();
      });
    });
  }

  // Product detail gallery thumbs
  document.querySelectorAll('[data-product-gallery]').forEach((gallery) => {
    const main = gallery.querySelector('#productMainImage');
    if (!main) return;
    gallery.querySelectorAll('.product-gallery-thumb').forEach((btn) => {
      btn.addEventListener('click', () => {
        const src = btn.getAttribute('data-src');
        if (!src) return;
        main.style.opacity = '0';
        window.setTimeout(() => {
          main.src = src;
          main.style.opacity = '1';
        }, 140);
        gallery.querySelectorAll('.product-gallery-thumb').forEach((b) => b.classList.remove('is-active'));
        btn.classList.add('is-active');
      });
    });
    main.style.transition = 'opacity .2s ease';
  });

  // Admin product image previews (create/edit)
  const mainInput = document.getElementById('mainImageInput');
  const mainWrap = document.getElementById('mainImagePreviewWrap');
  const mainImg = document.getElementById('mainImagePreview');
  if (mainInput && mainWrap && mainImg) {
    mainInput.addEventListener('change', () => {
      const file = mainInput.files && mainInput.files[0];
      if (!file) {
        mainWrap.classList.add('d-none');
        return;
      }
      mainImg.src = URL.createObjectURL(file);
      mainWrap.classList.remove('d-none');
    });
  }

  const galleryInput = document.getElementById('galleryImageInput');
  const galleryPreview = document.getElementById('galleryPreview');
  if (galleryInput && galleryPreview) {
    galleryInput.addEventListener('change', () => {
      galleryPreview.innerHTML = '';
      Array.from(galleryInput.files || []).forEach((file) => {
        const item = document.createElement('div');
        item.className = 'product-admin-gallery-item';
        const img = document.createElement('img');
        img.src = URL.createObjectURL(file);
        img.alt = '';
        item.appendChild(img);
        galleryPreview.appendChild(item);
      });
    });
  }
})();
