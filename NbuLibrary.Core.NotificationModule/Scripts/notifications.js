define('mods/notifications',
    ['jquery', 'navigation', 'router', 'textprovider', 'presenter', 'messanger', 'dataservice', 'usercontext', 'home', 'mods/vm.notifications', 'mods/vm.notification'],
    function ($, navigation, router, tp, presenter, msgr, dataservice, usercontext, home, vmNotifList, vmNotif) {

        var item = navigation.add(tp.get('nav_notifications_root'), '#/notif', null, 'icon-envelope');
        item.add(tp.get('nav_notifications_inbox'), '#/notif/inbox/');
        item.add(tp.get('nav_notifications_sent'), '#/notif/sent/');

        router.add('#/notif/inbox/', function () {
            vmNotifList.bind('inbox');
            presenter.show(vmNotifList);
        });

        //TODO: not the right place for this
        router.add('#/notif/related/:entity/:role/:id', function () {
            vmNotifList.bind('custom', {entity: this.entity, role: this.role, id: this.id});
            presenter.show(vmNotifList);
        });

        router.add('#/notif/sent/', function () {
            vmNotifList.bind('sent');
            presenter.show(vmNotifList);
        });
        router.registerDetailsPage('Notification', '#/notif/read/:id');
        router.add('#/notif/read/:id', function () {
            vmNotif.bind(this.id);
            presenter.show(vmNotif);
        });

        var q = dataservice.search('Notification');
        q.rules.is('Received', false);
        q.rules.relatedTo('User', 'Recipient', usercontext.currentUser().Id);
        $.when(q.execute()).done(function (res) {
            if (res.length) {
                home.addInfo('Съобщения', 'Имате <a href="#/notif/inbox/"> непрочетени съобщения (' + res.length + ')</a>.', 'envelope');
            }
        });
    });