define('mods/vm.files',
    ['jquery', 'usercontext', 'dataservice', 'textprovider', 'presenter', 'grid', 'filters'],
    function ($, usercontext, dataservice, tp, presenter, entitygrid, gf) {

        var MODULE_ID = 4;
        var Permissions = {
            ManageOwn: 'ManageOwn',
            ViewOwn: 'ViewOwn',
            ManageAll: 'ManageAll'
        };

        var FileAccessType =
        {
            None: 0,
            Owner: 1,
            Full: 2,
            Temporary: 3,
            Token: 4
        }

        var ViewTypes = {
            Textfield: 0,
            Numberfield: 1,
            Datefield: 2,
            Enumfield: 3,
            Checkfield: 4
        };

        var grid = new entitygrid.Grid();
        var filters = gf.GridFilters();

        function bind(all) {
            var query = dataservice.search('File');
            query.sort('CreatedBy', true);
            if (!all) {
                var relq = query.rules.relatedTo('User', 'Access', usercontext.currentUser().Id);
                relq.rules.relation.is('Type', FileAccessType.Owner);
            }

            filters.bind([
                { Type: 2, Label: tp.get('filterby_filename'), Property: 'Name' }
            ],
                query, function () { grid.reload(true); }, function () { grid.reload(true); });

            grid.bind(query, 'Files_Grid');
        }
        function remove(file) {
            var ask = presenter.ask(tp.get('confirm_entity_deletion'));
            ask.yes(function () {
                $.when(dataservice.remove('File', file.Id)).done(function () {
                    grid.Items.remove(file);
                });
            });
            ask.show();
        }

        return {
            bind: bind,
            template: 'Files_Grid',
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            remove: remove,
            sort: grid.sort,
            //create: create
        };
    });
