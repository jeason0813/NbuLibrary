define('mods/vm.inquery',
    ['jquery', 'ko', 'textprovider', 'datacontext', 'messanger', 'presenter', 'viewservice', 'entitypage', 'usercontext'],
    function ($, ko, tp, datacontext, msgr, presenter, vs, EntityPage, usercontext) {

        var INQUERY = 'Inquery';

        var page = new EntityPage.Page();
        var template = ko.observable();
        var _id = ko.observable();
        function bind(id, edit) {
            _id(id);
            if (!edit) {
                template('GeneralDetails');
                var uiDef = 'AskTheLib_Librarian_InqueryDetails';
                if (usercontext.isCustomer())
                    uiDef = 'AskTheLib_Customer_InqueryDetails';

                page.bind(INQUERY, uiDef, id);
                page.load();
            }
            else {
                if (usercontext.isCustomer()) {
                    template('AskTheLib_Customer_InqueryForm');
                    page.bind('Inquery', 'AskTheLib_Customer_InqueryForm', _id());
                }
                else {
                    template('GeneralForm');
                    page.bind('Inquery', 'AskTheLib_Librarian_InqueryForm', _id());
                }
                var job = null;
                if (_id())
                    job = page.load();
                else
                    job = page.loadDefaults();
                $.when(job).done(function () { datacontext.bind(INQUERY, page.Entity, _id()); });
            }
        };
        function save() {
            if (datacontext.hasChanges() && presenter.getMainForm().valid())
                $.when(datacontext.save()).done(function () {
                    msgr.success(tp.get('msg_query_created'));
                    if (usercontext.isCustomer())
                        window.location = '#/askthelib/inqueries/';
                });
        }
        function cancel() {
            //TODO: cancel
            if (usercontext.isCustomer())
                window.location = '#/askthelib/inqueries/';
        }

        return {
            bind: bind,
            template: template,
            Loaded: page.Loaded,
            LoadedDef: page.LoadedDef,
            Definition: page.Definition,
            Entity: page.Entity,
            NoAccess: page.NoAccess,
            Id: _id,
            save: save,
            cancel: cancel,
            IsDirty: datacontext.hasChanges
        };
    });

