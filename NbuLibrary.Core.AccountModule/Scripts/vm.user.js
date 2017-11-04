define('mods/vm.user',
    ['jquery', 'ko', 'dataservice', 'viewservice', 'entitymapper', 'messanger', 'presenter', 'textprovider', 'datacontext', 'entitypage'],
    function ($, ko, dataservice, viewservice, mapper, msgr, presenter, tp, dc, EntityPage) {
        var USER = 'User';

        var editMode = ko.observable(false);
        var entityId = ko.observable();
        var page = new EntityPage.Page();

        function bind(id, openEdit) {

            //cancel();
            openEdit = !!openEdit;
            entityId(id);

            if (!openEdit) {
                page.bind(USER, 'Account_Admin_User', id);
                page.load();
            }
            else
                edit();

            //todo: isdirty!
        };

        function edit() {
            //loadedDef(false);
            page.bind(USER, 'Account_Admin_UserForm', entityId());
            editMode(true);
            $.when(page.load()).done(function () { dc.bind(USER, page.Entity, entityId()); });

        };

        function cancel() {
            //todo: reset changes
            //loadedDef(false);
            page.bind(USER, 'Account_Admin_User', entityId());
            editMode(false);
            page.load();

        };

        function save() {
            if (dc.hasChanges() && presenter.getMainForm().valid()) {
                $.when(dc.save())
                .done(function (res) {
                    dc.clear();
                    msgr.success(tp.get('msg_saved_ok'));
                    page.bind(USER, 'Account_Admin_User', entityId());
                    editMode(false);
                    page.load();
                });
            }
        };



        return {
            template: 'Account_Admin_User',
            bind: bind,
            Loaded: page.Loaded,
            LoadedDef: page.LoadedDef,
            NoAccess: page.NoAccess,
            Definition: page.Definition,
            Entity: page.Entity,
            EditMode: editMode,
            IsDirty: dc.hasChanges,
            edit: edit,
            cancel: cancel,
            save: save
        };
    });



