define('mods/vm.mymagcustomers',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'datacontext', 'messanger', 'grid', 'usercontext', 'entitypage', 'presenter', 'filters'],
    function ($, ko, tp, dataservice, datacontext, msgr, entitygrid, usercontext, ep, presenter, gf) {

        var query = null;
        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();

        var _bind=false;
        function bind() {
            if (!_bind) {
                query = dataservice.search('User');
                query.rules.is('UserType', 0);

                filters.bind([
                   { Multiple: false, Entity: 'UserGroup', Role: 'UserGroup', Type: 4, Formula: '{Name}', Label: tp.get('filterby_usergroup') },
                   { Multiple: false, Type: 2, Label: tp.get('filterby_user_fullname'), Property: 'FullName' },
                   { Multiple: false, Type: 2, Label: tp.get('filterby_user_email'), Property: 'Email' }
                ],
                    query, function () { grid.reload(true); }, function () { grid.reload(true); });
                grid.bind(query, 'MyMagazines_Librarian_UsersGrid');
                _bind = true;
            }
            else
                grid.reload();
        };


        function createUser() {
            var page = new ep.Page();
            page.bind('User', 'MyMagazines_Librarian_UserForm');
            page.template = 'GeneralForm';
            $.when(page.loadDefaults()).done(function () {
                datacontext.bind('User', page.Entity);
            });

            var popup = presenter.popup(page, { title: tp.get('heading_mymagazines_addcustomer') });
            popup.ok(tp.get('btn_save'), function () {
                if (datacontext.hasChanges() && popup.getForm().valid()) {
                    datacontext.set('UserType', 0);//Customer
                    datacontext.set('IsActive', true);
                    $.when(datacontext.save())
                    .done(function (res) {
                        datacontext.clear();
                        msgr.success(tp.get('msg_saved_ok'));
                        popup.close();
                        grid.reload();
                    });
                }
            });
            popup.show();
        };

        return {
            template: 'MyMagazines_Librarian_UsersGrid',
            bind: bind,
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            create: createUser,
            sort: grid.sort,
            canEdit: usercontext.hasPermission(1, 'UserManagement') //account module
        };
    });




