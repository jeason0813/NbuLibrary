define('uitexts',
    ['jquery', 'textprovider', 'navigation', 'router', 'messanger', 'presenter', 'usercontext'],
    function ($, tp, nav, router, msgr, presenter, usercontext) {

        var vmTexts = (function () {

            var uitextsRaw = [];
            var filter = ko.observable('');

            $.each(tp.all, function (key, value) {
                var item = { Key: key, Value: ko.observable(value), Dirty: ko.observable(false) };
                item.Value.subscribe(function () {
                    item.Dirty(true);
                });

                uitextsRaw.push(item);
            });

            var uiTexts = ko.observableArray(uitextsRaw);

            function save(uitext) {
                if (uitext.Dirty()) {
                    $.when(tp.set(uitext.Key, uitext.Value()))
                    .done(function () {
                        msgr.success(tp.get('msg_saved_ok'));
                        uitext.Dirty(false);
                    })
                }

            };
            function add() {
                var item = { Key: filter(), Value: ko.observable(filter()), Dirty: ko.observable(true) };
                item.Value.subscribe(function () {
                    item.Dirty(true);
                });
                uiTexts.push(item);
            };
            function remove(uitext) {
                var ask = presenter.ask('Are you sure you want to delete the UI text "' + uitext.Key + '"?');
                ask.yes(function () {
                    $.when(tp.unset(uitext.Key))
                       .done(function () {
                           msgr.success(tp.get('msg_saved_ok'));
                           uiTexts.remove(uitext);
                       });
                });
                ask.show();
            };

            return {
                Filter: filter,
                UITexts: uiTexts,
                filteredUITexts: ko.computed(function () {
                    var term = filter().toLowerCase();
                    if (!filter)
                        return uiTexts();
                    else
                        return ko.utils.arrayFilter(uiTexts(), function (item) {
                            return ko.utils.stringStartsWith(item.Key.toLowerCase(), term);
                        });
                }),
                template: 'Admin_UITexts',
                set: save,
                add: add,
                remove: remove
            };
        })();

        if (usercontext.isAdmin()) {
            nav.add(tp.get('nav_uitexts'), '#/uitexts/');
            router.add('#/uitexts/', function () {
                presenter.show(vmTexts);
            });
        }
    });