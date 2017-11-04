define('mods/vm.analysisquery',
    ['dataservice', 'datacontext', 'usercontext', 'presenter', 'textprovider', 'entitypage', 'messanger'],
    function (dataservice, datacontext, usercontext, presenter, tp, ep, msgr) {
        var EntityConsts = {
            AnalysisQuery: 'AnalysisQuery'
        };

        var page = new ep.Page();

        function bind(id) {
            page.bind(EntityConsts.AnalysisQuery, usercontext.isCustomer() ? 'RefAnalysis_Customer_AnalysisQuery_Form' : 'RefAnalysis_Librarian_AnalysisQuery_Form', id);


            var job = id ? page.load() : page.loadDefaults();
            $.when(job).done(function () {
                datacontext.bind(EntityConsts.AnalysisQuery, page.Entity, id);
            });
        };

        function save() {
            if (datacontext.hasChanges() && presenter.getMainForm().valid())
                $.when(datacontext.save()).done(function () {
                    msgr.success(tp.get('msg_query_created'));
                    window.location = '#/refanalysis/queries';
                });
        };

        return {
            template: 'RefAnalysis_Customer_QueryForm',
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