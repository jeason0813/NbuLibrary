define('mods/vm.bibllistquery',
    ['dataservice', 'datacontext', 'usercontext', 'presenter', 'textprovider', 'entitypage', 'messanger'],
    function (dataservice, datacontext, usercontext, presenter, tp, ep, msgr) {
        var EntityConsts = {
            BibliographicListQuery: 'BibliographicListQuery',
            BibliographicListStandart: 'BibliographicListStandart'
        };

        var page = new ep.Page();

        function bind(id) {
            page.bind(EntityConsts.BibliographicListQuery, usercontext.isCustomer() ? 'BiblList_Customer_BibliographicListQuery_Form' : 'BiblList_Librarian_BibliographicListQuery_Form', id);


            var job = id ? page.load() : page.loadDefaults();
            $.when(job).done(function () {
                datacontext.bind(EntityConsts.BibliographicListQuery, page.Entity, id);
            });
        };

        function save() {
            if (datacontext.hasChanges() && presenter.getMainForm().valid())
                $.when(datacontext.save()).done(function () {
                    msgr.success(tp.get('msg_query_created'));
                    window.location = '#/bibllist/queries';
                });
        };

        return {
            template: 'BiblList_Customer_QueryForm',
            bind: bind,
            Loaded: page.Loaded,
            LoadedDef: page.LoadedDef,
            Definition: page.Definition,
            Entity: page.Entity,
            NoAccess: page.NoAccess,
            IsDirty: datacontext.hasChanges,
            save: save
        };
    });