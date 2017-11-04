define('mods/vm.notification',
    ['jquery', 'textprovider', 'presenter', 'messanger', 'dataservice', 'usercontext', 'entitypage'],
    function ($, tp, presenter, msgr, dataservice, uc, EntityPage) {

        var page = new EntityPage.Page();

        function bind(id) {
            page.bind('Notification', 'Notifications_All_Read', id);
            $.when(page.load())
            .done(function () {
                if (page.Entity.RelationsData.User_Recipient
                    && page.Entity.RelationsData.User_Recipient()
                    && page.Entity.RelationsData.User_Recipient().Entity.Id === uc.currentUser().Id) {
                    var update = dataservice.update('Notification', id);
                    update.set('Received', true);
                    update.execute();
                }
            });
        }

        return {
            bind: bind,
            template: 'GeneralDetails',
            Loaded: page.Loaded,
            LoadedDef: page.LoadedDef,
            Definition: page.Definition,
            Entity: page.Entity,
            NoAccess: page.NoAccess
        };
    });
