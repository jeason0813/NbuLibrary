define('mods/vm.magazinemgr',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'datacontext', 'messanger', 'grid', 'entitypage', 'usercontext', 'entitymapper', 'presenter', 'filters'],
    function ($, ko, tp, ds, dc, msgr, entitygrid, entitypage, usercontext, mapper, presenter, gf) {
        var template = 'MyMagazines_Librarian_Issues';

        var _loaded = ko.observable(false);
        var _query = null;
        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();
        var _magazineId = ko.observable();
        var _magazineTitle = ko.observable();

        function bind(id) {
            if (_magazineId() !== id) {
                _loaded(false);
                _magazineTitle('');
                _magazineId(id);
                _query = ds.search('Issue').allProperties(true);
                _query.include('File', 'Content');
                _query.rules.relatedTo('Magazine', 'Issue', id);
                _query.sort('CreatedOn', true);

                filters.bind([{ Type: 6, Label: tp.get('filterby_issue_year'), Property: 'Year' }],
                          _query,
                          function () { grid.reload(true); }, //filters apply callback
                          function () { grid.reload(true); });//filters clear callback
                grid.bind(_query, 'MyMagazines_Librarian_IssuesGrid');

                var magQ = ds.get('Magazine', id);
                magQ.addProperty('Title');
                $.when(magQ.execute()).done(function (mag) { _magazineTitle(mag.Data.Title); });
            }
            else
                grid.reload();
            //$.when(_query.execute()).done(function (raw) {
            //    $.when(mapper.map('Magazine', raw, _mag)).done(function () {
            //        _loaded(true);
            //    });
            //});
        };

        function openForm(id) {
            var page = new entitypage.Page();
            if (id)
                page.bind('Issue', 'MyMagazines_Librarian_IssueForm', id);
            else
                page.bind('Issue', 'MyMagazines_Librarian_IssueForm');

            page.template = 'GeneralForm';

            var job = id ? page.load() : page.loadDefaults();
            $.when(job).done(function () {
                dc.bind('Issue', page.Entity, id);
                if (!id) {
                    page.Entity = dc.getEntity();
                    dc.addRelation('Magazine', 'Issue', _magazineId());
                }
                var popup = presenter.popup(page, { title: tp.get('entity_issue') });
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

        function send(issue)
        {
            var update = ds.update('Issue', issue.Id);
            update.set('Sent', true);
            $.when(update.execute()).done(function () {
                msgr.success(tp.get('msg_issue_sent'));
                grid.reload();
            });
        }

        return {
            bind: bind,
            template: template,
            Title : _magazineTitle,
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters : filters,
            sort: grid.sort,
            create: function () { openForm(); },
            edit: function (issue) { openForm(issue.Id); },
            send: send
            //canEdit: canEdit,
            //canCancel: canEdit,
            //process: process,
            //cancel: cancel,
            //create: create,
            //viewNotifications: viewNotifications
        };
    });



