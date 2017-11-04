define('profile',
    ['navigation', 'router', 'messanger', 'textprovider', 'presenter', 'dataservice', 'usercontext', 'vm.profileupdate'],
    function (navigation, router, msgr, tp, presenter, dataservice, usercontext, vmProfileUpdate) {
        var item = navigation.add(tp.get('nav_profile'), '#/profile/', 9999, 'icon-user');
        item.add(tp.get('nav_profile_changepass'), '#/profile/changepass', 1, ' icon-lock');
        item.add(tp.get('nav_profile_updateinfo'), '#/profile/updateinfo', 1, ' icon-wrench');
        item.add(tp.get('nav_logout'), '/Login/SignOut', 9999, ' icon-ban-circle');

        router.add('#/profile/changepass', function () {
            var vm = {
                template: 'Profile_ChangePassword',
                pass: ko.observable(),
                confirm: ko.observable()
            };

            var dialog = presenter.popup(vm);
            dialog.ok(tp.get('btn_change_password'), function () {
                if (vm.pass() != vm.confirm())
                    msgr.error(tp.get('msg_password_confirm_failed'));
                else {
                    var update = dataservice.update('User', usercontext.currentUser().Id);
                    update.set('Password', vm.pass());
                    $.when(update.execute()).done(function () {
                        msgr.success('msg_password_change_success');
                        dialog.close();
                    });
                }
            });
            dialog.show();
        });

        router.add('#/profile/updateinfo', function () {
            vmProfileUpdate.bind(usercontext.currentUser().Id);
            presenter.show(vmProfileUpdate);
        })
    });