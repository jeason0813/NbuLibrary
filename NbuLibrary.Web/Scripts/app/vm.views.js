define('vm.views',
	['ko', 'viewservice'],
	function (ko, vs) {

		var UITypes = {
			'Grid': 0,
			'Form': 1,
			'View': 2
		};

		var UITypesNames = {
			0: 'Grid',
			1: 'Form',
			2: 'View',
		}



		var loaded = ko.observable(false);
		var views = ko.observableArray([]);

		$.when(vs.getAll())
			.done(function (data) {
				//todo: optimize array feed
				$(data).each(function () {
					this.Type = UITypesNames[this.Type];
					views.push(this);
				});
				loaded(true);
			});


		return {
			template : 'AdministerViews',
			Views: views,
			Loaded: loaded
		};


	});