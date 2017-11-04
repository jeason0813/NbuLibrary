define('mods/filesmodule',
    ['jquery', 'usercontext', 'navigation', 'router', 'textprovider', 'presenter', 'mods/vm.files', 'mods/vm.file'],
    function ($, usercontext, navigation, router, tp, presenter, vmFiles, vmFile) {

        var MODULE_ID = 4;
        var Permissions = {
            ManageOwn: 'ManageOwn',
            ViewOwn: 'ViewOwn',
            ManageAll: 'ManageAll'
        };

        router.add('#/filedownload/:id', function () {

            var fileId = this.id;
            $.when($.get('File/CheckAccess', { id: fileId })).done(function (response) {
                if (response.hasAccess) {
                    window.location = 'file/download?id=' + fileId;
                }
                var vm = {
                    template: 'Files_Downloaded',
                    message: tp.get(response.hasAccess ? 'msg_file_downloaded_success' : 'msg_file_downloaded_noaccess'),
                    ok: response.hasAccess
                };
                presenter.show(vm);
            });
        });

        var rootItem = null;
        if (usercontext.hasPermission(MODULE_ID, Permissions.ManageAll)
            || usercontext.hasPermission(MODULE_ID, Permissions.ManageOwn)
            || usercontext.hasPermission(MODULE_ID, Permissions.ViewOwn)) {
            rootItem = navigation.add(tp.get('nav_files_root'), '#/files');
        }

        if (usercontext.hasPermission(MODULE_ID, Permissions.ManageAll)
            || usercontext.hasPermission(MODULE_ID, Permissions.ManageOwn)) {
            router.add('#/file/:id', function () {
                vmFile.bind(this.id);
                presenter.show(vmFile);
            });
        }

        if (usercontext.hasPermission(MODULE_ID, Permissions.ManageAll)) {
            var url = '#/files/all';
            rootItem.add(tp.get('nav_files_all'), url);
            router.add(url, function () {
                vmFiles.bind(true);
                presenter.show(vmFiles);
            });
        }

        if (usercontext.hasPermission(MODULE_ID, Permissions.ManageOwn)) {
            var url = '#/files/mine';
            rootItem.add(tp.get('nav_files_own'), url);
            router.add(url, function () {
                vmFiles.bind(false);
                presenter.show(vmFiles);
            });
        }
        //TODO: filesmodule - ui - viewown files
        //if (usercontext.hasPermission(MODULE_ID, Permissions.ViewOwn)) {
        //    var url = '#/files/view';
        //    rootItem.add(tp.get('nav_files_view'), url);
        //    router.add(url, function () {
        //        console.log('not implemented');
        //    });
        //}
    });
