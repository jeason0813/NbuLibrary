define('viewmgr',
    ['navigation', 'router', 'presenter', 'textprovider', 'usercontext', 'vm.views', 'vm.view'],
    function (nav, router, presenter, tp, usercontext, vmViews, vmView) {

        if (usercontext.isAdmin()) {
            router.add('#/views', function () {
                presenter.show(vmViews);
            });

            router.add('#/views/:name', function () {
                vmView.bind(this.name);
                presenter.show(vmView); 
            });

            nav.add(tp.get('nav_views'), '#/views');
        }
    });