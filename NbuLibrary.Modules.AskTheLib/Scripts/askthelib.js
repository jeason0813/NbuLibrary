/// <reference path="../app/usercontext.js" />
define('mods/askthelib',
    ['jquery', 'navigation', 'router', 'textprovider', 'presenter', 'messanger', 'usercontext', 'entitypage', 'datacontext', 'mods/vm.inqueries', 'mods/vm.inquery', 'reports'],
    function ($, navigation, router, tp, presenter, msgr, usercontext, EntityPage, datacontext, vmInqueries, vmInquery, reports) {
        var MODULE_ID = 101;
        var Permissions = {
            Use: 'Use'
        };

        if (!usercontext.hasPermission(MODULE_ID, Permissions.Use))
            return;

        var item = navigation.add(tp.get('nav_askthelib'), '#/askthelib/inqueries/');

        if (usercontext.isCustomer()) {
            item.add(tp.get('nav_askthelib_newquery'), '#/askthelib/inqueries/new/');
            item.add(tp.get('nav_askthelib_inqueries'), '#/askthelib/inqueries/');
        }

        if (usercontext.isLibrarian()) {
            $.when(reports.getByModule(MODULE_ID)).done(function (reports) {
                if (reports && reports.length)
                    item.add(tp.get('nav_askthelib_inqueries'), '#/askthelib/inqueries/');
                $(reports).each(function () {
                    item.add(this.label, this.url, 9999, 'icon-list-alt');
                });
            });
        }


        router.add('#/askthelib/inqueries/', function () {
            vmInqueries.bind();
            presenter.show(vmInqueries);
        });

        router.registerDetailsPage('Inquery', '#/askthelib/inqueries/:id');
        router.add('#/askthelib/inqueries/:id', function () {
            vmInquery.bind(this.id)
            presenter.show(vmInquery);
        });
        router.add('#/askthelib/inqueries/edit/:id', function () {
            vmInquery.bind(this.id, true);
            presenter.show(vmInquery);
        });

        if (usercontext.isCustomer()) {
            router.add('#/askthelib/inqueries/new/', function () {
                vmInquery.bind(null, true);
                presenter.show(vmInquery);
            });
        }

        //router.add('#/account/users/edit/:id', function () {
        //    vmUser.bind(this.id);
        //    vmUser.edit();
        //    presenter.show(vmUser);
        //});
    });

