define('mods/vm.magazines',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'datacontext', 'messanger', 'grid', 'usercontext', 'entitypage', 'presenter', 'filters'],
    function ($, ko, tp, ds, dc, msgr, entitygrid, usercontext, ep, presenter, gf) {

        var query = null;
        var template = usercontext.isCustomer() ? "MyMagazines_Customer_MagazinesGrid" : "MyMagazines_Librarian_MagazinesGrid";

        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();
        var _bind = false;
        function bind() {

            if (!_bind) {
                query = ds.search('Magazine');

                filters.bind([
                    { Type: 5, Label: tp.get('filterby_isactive'), Property: 'IsActive', YesLabel: tp.get('filterby_isactive_yes'), NoLabel: tp.get('filterby_isactive_no') },
                    { Multiple: false, Type: 7, Property: 'Title', Label: tp.get('filterby_magazinetitle') }
                ],
                    query, function () { grid.reload(true); }, function () { grid.reload(true);});
                filters.Filters()[0].onChange({ added: { id: 2 } });
                var uiDef = 'MyMagazines_Librarian_MagazinesGrid';
                if (usercontext.isCustomer()) {
                    uiDef = 'MyMagazines_Customer_MagazinesGrid';
                }

                grid.bind(query, uiDef);
                _bind = true;
            }
            else
                grid.reload();
        };

        function openForm(id) {
            var page = new ep.Page();
            if (id)
                page.bind('Magazine', 'MyMagazines_Librarian_MagazineForm', id);
            else
                page.bind('Magazine', 'MyMagazines_Librarian_MagazineForm');

            page.template = 'GeneralForm';

            var job = id ? page.load() : page.loadDefaults();
            $.when(job).done(function () {
                dc.bind('Magazine', page.Entity, id);
                var popup = presenter.popup(page, { title: tp.get('entity_magazine') });
                popup.ok(tp.get('btn_save'), function () {
                    $.when(dc.save()).done(function () {
                        popup.close();
                        msgr.success(tp.get('msg_saved_ok'));
                        grid.reload();
                    });
                });
                popup.show();
            });
        }

        function addSubscription(magazine) {
            var vm = (function () {
                var _selectedUser = ko.observable();
                return {
                    template: 'MyMagazines_Librarian_AddSubscriptions',
                    UsersAjax: {
                        url: "api/entity/search",
                        type: 'post',
                        dataType: 'json',
                        data: function (term, page) {
                            var q = dataservice.search('User');
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
                var update = dataservice.update('Magazine', magazine.Id);
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

        return {
            bind: bind,
            template: template,
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            sort: grid.sort,
            create: function () { openForm(); },
            edit: function (magazine) { openForm(magazine.Id); }
            //canEdit: canEdit,
            //canCancel: canEdit,
            //process: process,
            //cancel: cancel,
            //create: create,
            //viewNotifications: viewNotifications
        };
    });




