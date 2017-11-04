define('mods/vm.bibliography',
    ['entitypage', 'dataservice', 'datacontext', 'usercontext'],
    function (ep, ds, dc, usercontext) {
        var EntityConsts = {
            Bibliography: 'Bibliography',
            BibliographicDocument: 'BibliographicDocument',
            Language: 'Language',
            BibliographicQuery: 'BibliographicQuery'
        };

        var page = new ep.Page();
        var _keywords = ko.observableArray();
        var _id = ko.observable();

        function bind(id) {
            _keywords.removeAll();
            page.bind(EntityConsts.Bibliography, usercontext.isCustomer() ? 'BiblRef_Bibliography_Form' : 'BiblRef_Librarian_Bibliography_Form', id);
            var job = null;
            if (id)
                job = page.load();
            else
                job = page.loadDefaults()
            _id(id);
            $.when(job).done(function () {
                dc.bind(EntityConsts.Bibliography, page.Entity, id);
                if (page.Entity.RelationsData.Language_Keywords)
                    ko.utils.arrayForEach(page.Entity.RelationsData.Language_Keywords(), function (kwrds) {
                        _keywords.push({ Id: kwrds.Entity.Id, Language: kwrds.Entity.Data.Value, Keywords: ko.observable(kwrds.Data.Keywords), Saved: kwrds.Data.Keywords });
                    });
            });
        }

        function getUpdate() {
            var update = ds.update(EntityConsts.Bibliography, _id());
            dc.applyChanges(update);
            ko.utils.arrayForEach(_keywords(), function (kw) {
                if (kw.Saved !== kw.Keywords()) {
                    var relUpdate = update.getRelationUpdate('Language', 'Keywords', kw.Id);
                    if (!relUpdate)
                        relUpdate = update.updateRelation('Language', 'Keywords', kw.Id);
                    relUpdate.set('Keywords', kw.Keywords());
                }
            });
            return update;
        }

        function onChangeLanguage(e) {
            //console.log('lang', e);
            if (e.added) {
                var kw = { Id: e.added.id, Language: e.added.text, Keywords: ko.observable(), Saved: undefined };
                _keywords.push(kw);
                dc.addRelation(EntityConsts.Language, 'Keywords', e.added.id);
            }
            else if (e.removed) {
                var kw = ko.utils.arrayFirst(_keywords(), function (x) { return x.Id == e.removed.id; });
                if (kw) {
                    _keywords.remove(kw);
                    dc.removeRelation(EntityConsts.Language, 'Keywords', kw.Id);
                }
            }
        }

        return {
            template: 'BiblRef_Customer_BibliographyForm',
            bind: bind,
            Loaded: page.Loaded,
            LoadedDef: page.LoadedDef,
            Definition: page.Definition,
            Keywords: _keywords,
            onChangeLanguage: onChangeLanguage,
            KeywordsField: ko.computed(function () {
                if (!page.LoadedDef())
                    return null;
                return ko.utils.arrayFirst(page.Definition().Fields, function (f) { return f.Entity == 'Language'; });
            }),
            Entity: page.Entity,
            NoAccess: page.NoAccess,
            getUpdate: getUpdate
        };
    });
