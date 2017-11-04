/// <reference path="../knockout-2.2.0.debug.js" />

define('users',
	['jquery', 'router', 'dataservice', 'presenter', 'navigation', 'textProvider', 'vm.user'],
	function ($, router, dataservice, presenter, nav, tp, vmUser) {

		nav.add(tp.get('nav_users'), '#/users/');
		nav.add(tp.get('nav_usergroups'), '#/usergroups/');

		router.add('#/users/', function () {
			$.when(dataservice.search('User'))
				.done(function (res) {
					var vm = {
						Users: ko.observableArray(res),
						template: function () {
							return 'core_users';
						}
					};
					presenter.show(vm);
				});
		});
		router.add('#/usergroups/', function () {
			$.when(dataservice.search('UserGroup'))
				.done(function (items) {
					var vm = {
						UserGroups: ko.observableArray(items),
						template: function () {
							return 'core_usergroups';
						}
					};
					presenter.show(vm);
				});
		});

		router.add('#/users/:id', function () {
		    vmUser.bind(this.id);
		    presenter.show(vmUser);
			//$.when(dataservice.get('User', this.id))
			//.done(function (user) {
			//	user.template = 'core_user';
			//	presenter.show(user);
			//});
		});
	});