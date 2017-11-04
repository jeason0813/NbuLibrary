define('entityservice',
    ['jquery'],
    function ($) {

        var _entities = {};
        var _enums = {};

        function getEntity(entity) {
            return $.Deferred(function (def) {
                if (_entities[entity]) {
                    def.resolve(_entities[entity]);
                    return;
                }

                $.ajax({
                    type: 'POST',
                    url: '/api/Entity/GetEntityModel?name=' + entity,
                    success: function (res) {
                        _entities[entity] = res;
                        def.resolve(res);
                    }
                });
            }).promise();
        };

        function getEnum(enumClass) {
            return $.Deferred(function (def) {
                if (_enums[enumClass]) {
                    def.resolve(_enums[enumClass]);
                    return;
                }

                $.ajax({
                    type: 'GET',
                    url: '/api/Entity/GetEnum?enumClass=' + enumClass,
                    success: function (res) {
                        _enums[enumClass] = res;
                        def.resolve(res);
                    }
                });
            }).promise();
        };


        return {
            getEntity: getEntity,
            getEnum: getEnum
        };
    });