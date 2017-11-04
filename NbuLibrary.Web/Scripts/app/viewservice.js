define('viewservice',
    ['jquery', 'ko'],
    function ($, ko) {

        var _hash = {};

        function getView(view) {
            return $.Deferred(function (def) {
                if (_hash[view]) {
                    def.resolve(_hash[view]);
                }
                else {
                    $.ajax({
                        type: 'GET',
                        url: '/api/ViewDef/Get',
                        data: { viewName: view },
                        success: function (viewDef) {
                            _hash[view] = viewDef;
                            def.resolve(viewDef);
                        }
                    });
                }
            }).promise();

        };

        function getAllViews() {
            return $.Deferred(function (def) {
                $.ajax({
                    type: 'GET',
                    url: '/api/ViewDef/All',
                    success: function (views) {
                        def.resolve(views);
                    }
                });
            });            
        };

        function saveView(view) {
            return $.Deferred(function (def) {
                $.ajax({
                    type: 'POST',
                    url: '/api/ViewDef/Save',
                    data: { Raw: JSON.stringify(view) },
                    success: function (res) {
                        _hash[view.Name] = null;
                        if (res.success)
                            def.resolve(res);
                    }
                });
            });
        };

        return {
            get: getView,
            getAll: getAllViews,
            save : saveView
        };
    });