define('mods/vm.subscriptions',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'datacontext', 'messanger', 'grid', 'usercontext', 'entitypage', 'presenter', 'mods/vm.magazinemgr'],
    function ($, ko, tp, ds, dc, msgr, entitygrid, usercontext, ep, presenter, vmMagazineMgr) {

        var query = null;
        var template = usercontext.isCustomer() ? "MyMagazines_Customer_Subscriptions" : "MyMagazines_Librarian_Subscriptions";
        var grid = new entitygrid.Grid();
        var _id = ko.observable();
        var _magazineTitle = ko.observable();
        function bind(id) {
            _id(id);
            _magazineTitle('');
            query = ds.search('User');
            query.include('Magazine', 'Subscriber');
            query.rules.relatedTo('Magazine', 'Subscriber', id);
            var uiDef = 'MyMagazines_Librarian_SubscriptionsGrid';
            if (usercontext.isCustomer()) {
                uiDef = 'MyMagazines_Customer_SubscriptionsGrid';
            }

            query.sort('Email', false, null, null);
            grid.bind(query, uiDef);

            var magQ = ds.get('Magazine', id);
            magQ.addProperty('Title');
            $.when(magQ.execute()).done(function (mag) { _magazineTitle(mag.Data.Title); });
        };

        function addSubscription() {
            var vm = (function () {
                var _selectedUser = ko.observable();
                return {
                    template: 'MyMagazines_Librarian_AddSubscription',
                    UsersAjax: {
                        url: "api/entity/search",
                        type: 'post',
                        dataType: 'json',
                        data: function (term, page) {
                            var q = ds.search('User');
                            q.allProperties(true);//todo: all props
                            q.rules.startsWith('FullName', term);
                            q.setPage(page, 10);
                            return q.getRequest();
                        },
                        results: function (data, page) {
                            var more = data.length == 10; // whether or not there are more results available

                            var res = [];
                            ko.utils.arrayForEach(data, function (item) {
                                res.push({ text: item.Data.FullName, id: item.Id });
                            });
                            // notice we return the value of more so Select2 knows if more results can be loaded
                            return { results: res };
                        }
                    },
                    User: _selectedUser,
                    onChangeUser: function (e) {
                        if (e.added)
                            _selectedUser(e.added.id);
                        else
                            _selectedUser(null);
                    }
                };
            })();

            var popup = presenter.popup(vm, { title: tp.get('heading_mymagazines_addsubscription') });
            popup.ok(tp.get('btn_add_subscription'), function () {
                var update = ds.update('Magazine', _id());
                var relUpdate = update.attach('User', 'Subscriber', vm.User());
                relUpdate.set('IsActive', true);
                $.when(update.execute()).done(function () {
                    popup.close();
                    msgr.success(tp.get('msg_saved_ok'));
                    grid.reload();
                });
            });
            popup.show();
        }

        function isActive(user) {
            var subscription = ko.utils.arrayFirst(user.RelationsData.Magazine_Subscriber(), function (s) { return s.Entity.Id == _id(); });
            return subscription.Data.IsActive;
        };

        function setActive(user, value) {
            var update = ds.update('Magazine', _id());
            var relUpdate = update.updateRelation('User', 'Subscriber', user.Id);
            relUpdate.set('IsActive', value);
            $.when(update.execute()).done(function () {
                msgr.success(tp.get('msg_saved_ok'));
                bind(_id());
            });
        };

        return {
            bind: bind,
            template: template,
            Title: _magazineTitle,
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            sort: grid.sort,
            addSubscription: addSubscription,
            isActive: isActive,
            activate: function (user) { setActive(user, true); },
            deactivate: function (user) { setActive(user, false); }
            //canEdit: canEdit,
            //canCancel: canEdit,
            //process: process,
            //cancel: cancel,
            //create: create,
            //viewNotifications: viewNotifications
        };
    });




