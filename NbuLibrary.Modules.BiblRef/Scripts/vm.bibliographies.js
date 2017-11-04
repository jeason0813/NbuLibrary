define('mods/vm.bibliographies',
    ['dataservice', 'usercontext', 'textprovider', 'presenter', 'messanger', 'grid', 'entitypage', 'filters', 'mods/vm.bibliography'],
    function (ds, usercontext, tp, presenter, msgr, entitygrid, ep, gf, vmBibl) {

        var EntityConsts = {
            Bibliography: 'Bibliography',
            BibliographicDocument: 'BibliographicDocument',
            Language: 'Language',
            BibliographicQuery: 'BibliographicQuery'
        };


        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();

        var _selected = ko.observable();

        var _bindForPicker = undefined;
        var _template = ko.observable();

        function bind(bindForPicker) {
            if (_bindForPicker === bindForPicker && bindForPicker !== undefined)
                grid.reload();
            else {
                var query = ds.search(EntityConsts.Bibliography);
                if (bindForPicker)
                    query.rules.is('Complete', true);
                //query.rules.relatedTo('User', 'Customer', usercontext.currentUser().Id);
                filters.bind([
                    { Multiple: true, Entity: 'Language', Role: 'Keywords', Type: 4, Formula: '{Value}', Label: tp.get('filterby_bibliography_language') },
                   { Multiple: false, Type: 2, Label: tp.get('filterby_bibliography_subject'), Property: 'Subject' }
                ],
                    query, function () { grid.reload(true); }, function () { grid.reload(true); });
                grid.bind(query, usercontext.isCustomer() ? 'BiblRef_Customer_BibliographyGrid' : 'BiblRef_Librarian_BibliographyGrid');
                _bindForPicker = bindForPicker;
                if (bindForPicker === true)
                    _template('BiblRef_Customer_BibliographyGrid');
                else
                    _template('BiblRef_Librarian_BibliographyGrid');
            }
        }

        function remove(bibl) {

            var ask = presenter.ask(tp.get('confirm_entity_deletion'));
            ask.yes(function () {
                $.when(ds.remove(EntityConsts.Bibliography, bibl.Id(), false))
                .done(function () {
                    grid.Items.remove(bibl);
                });
            });
            ask.show();
        };

        function edit(bibl) {
            vmBibl.bind(bibl.Id());
            var popup = presenter.popup(vmBibl, { title: tp.get('heading_biblref_bibliographycreate') });
            popup.ok(tp.get('btn_save'), function () {
                if (!popup.getForm().valid())
                    return;

                var validKeywords = true;
                ko.utils.arrayForEach(vmBibl.Keywords(), function (kw) {
                    if (!kw.Keywords() || kw.Keywords().length == 0)
                        validKeywords = false;
                });
                if (!validKeywords)
                    return msgr.error(tp.get('msg_should_fill_keywords'));

                $.when(vmBibl.getUpdate().execute()).done(function () {
                    msgr.success(tp.get('msg_saved_ok'))
                    popup.close();
                    grid.reload();
                });

            });
            popup.show();
        }

        var biblPage = new ep.Page();
        function open(bibl) {
            var q = ds.get(EntityConsts.Bibliography, bibl.Id());
            biblPage.bind(EntityConsts.Bibliography, 'BiblRef_Librarian_Bibliography_Details', bibl.Id());
            biblPage.template = 'BiblRef_Librarian_Bibliography_Details';
            biblPage.load();
            var popup = presenter.popup(biblPage, { title: tp.get('heading_biblref_bibliographydetails') });
            popup.show();
        }
        function create() {
            vmBibl.bind();
            var popup = presenter.popup(vmBibl, { title: tp.get('heading_biblref_bibliographycreate') });
            popup.ok(tp.get('btn_create_bibliography'), function () {
                if (!popup.getForm().valid())
                    return;

                var validKeywords = true;
                ko.utils.arrayForEach(vmBibl.Keywords(), function (kw) {
                    if (!kw.Keywords() || kw.Keywords().length == 0)
                        validKeywords = false;
                });
                if (!validKeywords)
                    return msgr.error(tp.get('msg_should_fill_keywords'));

                $.when(vmBibl.getUpdate().execute()).done(function () {
                    msgr.success(tp.get('msg_saved_ok'))
                    popup.close();
                    grid.reload();
                });

            });
            popup.show();
        }


        return {
            bind: bind,
            template: _template,
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            sort: grid.sort,
            Selected: _selected,
            remove: remove,
            edit: edit,
            open: open,
            create: create
        };
    });



