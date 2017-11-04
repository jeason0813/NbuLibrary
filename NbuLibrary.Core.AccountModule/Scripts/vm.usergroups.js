define('mods/vm.usergroups',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'viewservice', 'grid'],
    function ($, ko, tp, ds, vs, entitygrid) {

        var USERGROUP = 'UserGroup';
        var query = null;
        var grid = new entitygrid.Grid();
        function bind() //todo: params
        {
            query = ds.search(USERGROUP);
            query.sort('Name');
            grid.bind(query, 'Account_Admin_UserGroupGrid');
        };

        function remove(group) {
            $.when(ds.remove(USERGROUP, group.Id(), false))
            .done(function () {
                grid.Items.remove(group);
            });
        };

        return {
            bind: bind,
            template: 'Account_Admin_UserGroupsGrid',
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            remove: remove,
            sort: grid.sort
        };
    });

