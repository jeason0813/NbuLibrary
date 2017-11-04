define('mods/account',
    ['jquery', 'usercontext', 'navigation', 'router', 'textprovider', 'presenter', 'mods/vm.users', 'mods/vm.user', 'mods/vm.usergroups', 'mods/vm.usergroup', 'mods/vm.pendingUsers', 'reports'],
    function ($, usercontext, navigation, router, tp, presenter, vmUsers, vmUser, vmUserGroups, vmUserGroup, vmPendingUsers, reports) {

        var MODULE_ID = 1;
        var Permissions = {
            UserActivation: 'UserActivation',
            UserGroupManagement: 'UserGroupManagement',
            UserManagement: 'UserManagement'
        }


        if (!usercontext.hasPermission(MODULE_ID) || usercontext.isCustomer())
            return;

        var item = navigation.add(tp.get('nav_account_root'), '#/account');

        if (usercontext.hasPermission(MODULE_ID, Permissions.UserManagement)) {
            item.add(tp.get('nav_account_usrs'), '#/account/users/');

            router.add('#/account/users/', function () {
                vmUsers.bind();
                presenter.show(vmUsers);
            });

            $.when(reports.getByModule(MODULE_ID)).done(function (reports) {
                $(reports).each(function () {
                    item.add(this.label, this.url, 9999, 'icon-list-alt');
                });
            });
        }

        if (usercontext.hasPermission(MODULE_ID, Permissions.UserActivation) || usercontext.hasPermission(MODULE_ID, Permissions.UserManagement)) {
            router.registerDetailsPage('User', '#/account/users/:id');
            router.add('#/account/users/:id', function () {
                vmUser.bind(this.id);

                presenter.show(vmUser);
            });

            router.add('#/account/users/edit/:id', function () {
                vmUser.bind(this.id, true);
                presenter.show(vmUser);
            });
        }
        if (usercontext.hasPermission(MODULE_ID, Permissions.UserGroupManagement)) {
            item.add(tp.get('nav_account_usrgrps'), '#/account/usergroups/');

            router.add('#/account/usergroups/', function () {
                vmUserGroups.bind();
                presenter.show(vmUserGroups);
            });

            router.add('#/account/usergroups/:id', function () {
                vmUserGroup.bind(this.id);
                presenter.show(vmUserGroup);
            });

            router.add('#/account/usergroups/new/', function () {
                vmUserGroup.bind();
                presenter.show(vmUserGroup);
            });

        }

        if (usercontext.hasPermission(MODULE_ID, Permissions.UserActivation)) {
            item.add(tp.get('nav_account_pending'), '#/account/pending/');

            router.add('#/account/pending/', function () {
                vmPendingUsers.bind();
                presenter.show(vmPendingUsers);
            });
        }
    });
