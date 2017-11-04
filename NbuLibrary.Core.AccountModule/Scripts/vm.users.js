define('mods/vm.users',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'viewservice', 'grid', 'presenter', 'entitypage', 'datacontext', 'messanger', 'filters'],
    function ($, ko, tp, ds, vs, entitygrid, presenter, EP, datacontext, msgr, gf) {

        var USER = 'User';
        var query = null;

        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();
        var _bind = false;
        function bind() //todo: params
        {
            if (!_bind) {
                query = ds.search('User');

                filters.bind([
                    { Multiple: false, Entity: 'UserGroup', Role: 'UserGroup', Type: 4, Formula: '{Name}', Label: tp.get('filterby_usergroup') },
                    { Multiple: false, Type: 2, Label: tp.get('filterby_user_fullname'), Property: 'FullName' },
                    { Multiple: false, Type: 3, Label: tp.get('filterby_user_usertype'), Property: 'UserType', EnumClass: 'NbuLibrary.Core.Domain.UserTypes, NbuLibrary.Core.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' }
                ],
                    query, function () { grid.reload(true); }, function () { grid.reload(true); });

                grid.bind(query, 'Account_Admin_UsersGrid');
                _bind = true;
            }
            else
                grid.reload();
        };

        function remove(user) {

            var ask = presenter.ask(tp.get('confirm_entity_deletion'));
            ask.yes(function () {
                $.when(ds.remove(USER, user.Id(), false))
                .done(function () {
                    grid.Items.remove(user);
                });
            });
            ask.show();
        };

        function create() {
            var page = new EP.Page();
            page.bind(USER, 'Account_Admin_UserForm');
            page.template = 'GeneralForm';
            $.when(page.loadDefaults()).done(function () {
                datacontext.bind(USER, page.Entity);
            });


            var popup = presenter.popup(page, {title: tp.get('heading_account_usercreate')});
            popup.ok(tp.get('btn_save'), function () {
                if (datacontext.hasChanges()) {
                    $.when(datacontext.save())
                    .done(function (res) {
                        datacontext.clear();
                        msgr.success(tp.get('msg_saved_ok'));
                        popup.close();
                        bind();
                    });
                }
            });
            popup.show();
        };

        return {
            bind: bind,
            template: 'Account_UsersGrid',
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            remove: remove,
            sort: grid.sort,
            create: create
        };
    });

