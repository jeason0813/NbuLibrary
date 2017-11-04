define('mods/vm.pendingUsers',
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
                query = ds.search(USER);
                query.rules.is('IsActive', false);

                filters.bind([
                   { Multiple: false, Entity: 'UserGroup', Role: 'UserGroup', Type: 4, Formula: '{Name}', Label: tp.get('filterby_usergroup') },
                   { Multiple: false, Type: 2, Label: tp.get('filterby_user_email'), Property: 'Email' },
                   { Multiple: false, Type: 2, Label: tp.get('filterby_user_fullname'), Property: 'FullName' }
                ],
                   query, function () { grid.reload(true); }, function () { grid.reload(true); });

                grid.bind(query, 'Account_Admin_PendingRegistrations');
                _bind = true;
            }
            else
                grid.reload();
        };

        function approve(user) {
            var update = ds.update(USER, user.Id());
            update.set('IsActive', true);
            $.when(update.execute())
            .done(function () {
                grid.reload();
                msgr.success(tp.get('msg_saved_ok'));
            });
        };

        function reject(user) {
            $.when(ds.remove(USER, user.Id(), true))
            .done(function () {
                grid.reload();
                msgr.success(tp.get('msg_saved_ok'));
            });
        }

        return {
            bind: bind,
            template: 'Account_PendingUsersGrid',
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            sort: grid.sort,

            approve: approve,
            reject: reject
        };
    });

