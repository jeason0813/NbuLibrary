define('mods/vm.biblquery',
    ['dataservice', 'datacontext', 'usercontext', 'presenter', 'textprovider', 'entitypage', 'messanger', 'mods/vm.bibliography', 'mods/vm.bibliographies'],
    function (dataservice, datacontext, usercontext, presenter, tp, ep, msgr, vmBibl, vmPickBibl) {
        var EntityConsts = {
            Bibliography: 'Bibliography',
            BibliographicDocument: 'BibliographicDocument',
            Language: 'Language',
            BibliographicQuery: 'BibliographicQuery'
        };

        var _biblData = {
            create: null,
            has: ko.observable(false),
            subject: ko.observable('')
        };
        var _hasBibliography = ko.observable(false);

        var page = new ep.Page();

        function bind() {
            _biblData.create = null;
            _biblData.has(false);
            _biblData.subject('');
            page.bind(EntityConsts.BibliographicQuery, usercontext.isCustomer() ? 'BiblRef_Customer_BibliographicQuery_Form' : 'BiblRef_Librarian_BibliographicQuery_Form');
            page.loadDefaults();
        };

        function createBibliography() {
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

                _biblData.create = vmBibl.getUpdate();
                popup.close();
                _biblData.has(true);
                _biblData.subject(vmBibl.Entity.Data.Subject());
                datacontext.bind(EntityConsts.BibliographicQuery, page.Entity);
            });
            popup.show();
        };

        function pickExistinigBibliography() {
            vmPickBibl.bind(true);
            var popup = presenter.popup(vmPickBibl, { fullWidth: true, title: tp.get('heading_biblref_pickbibl') });
            popup.ok(tp.get('btn_pick_bibliography'), function () {
                popup.close();
                _biblData.has(true);
                _biblData.subject(vmPickBibl.Selected().Data.Subject());
                datacontext.bind(EntityConsts.BibliographicQuery, page.Entity);
                datacontext.addRelation(EntityConsts.Bibliography, 'Query', vmPickBibl.Selected().Id);
            });
            popup.show();
        };

        function save() {
            if (presenter.getMainForm().valid())
                if (_biblData.create) {
                    $.when(_biblData.create.execute()).done(function (result) {
                        if (!result.Success)
                            return;

                        var id = result.Data.Created;
                        datacontext.addRelation(EntityConsts.Bibliography, 'Query', id);
                        datacontext.set('ForNew', true);
                        $.when(datacontext.save()).done(function () {
                            msgr.success(tp.get('msg_query_created'));
                            window.location = '#/biblref/queries';
                        });
                    });
                }
                else {
                    datacontext.set('ForNew', false);
                    $.when(datacontext.save()).done(function () {
                        msgr.success(tp.get('msg_query_created'));
                        window.location = '#/biblref/queries';
                    });
                }
        };

        return {
            template: 'BiblRef_Customer_QueryForm',
            bind: bind,
            Loaded: page.Loaded,
            LoadedDef: page.LoadedDef,
            Definition: page.Definition,
            Entity: page.Entity,
            NoAccess: page.NoAccess,
            HasBibliography: _biblData.has,
            Subject: _biblData.subject,
            createBibl: createBibliography,
            pickBibl: pickExistinigBibliography,
            save: save
        };
    });


