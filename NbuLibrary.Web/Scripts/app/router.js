/// <reference path="../lib/RequireJS/require.debug.js" />

define('router',
    ['sammy', 'messanger', 'textprovider', 'presenter', 'home'],
    function (Sammy, msgr, tp, presenter, home) {

        var app = new Sammy.Application(function () {
            //route config
        });

        var _detailPages = {};

        function start() {
            app.get('#*/', function () {
                msgr.error(tp.get('msg_wrong_url_or_no_access'))
            });

            app.run('#/');

        }

        function addRoute(url, callback) {
            app.get(url, function (ctx) {
                callback.call(ctx.params);
            });
        }

        addRoute('#/', function () {
            presenter.show(home);
        });

        return {
            add: addRoute,
            registerDetailsPage: function (entity, url) {
                var key = entity.toLowerCase();
                if (_detailPages[key])
                    throw Error("Details page for entity '" + entity + "' already registered.");
                else
                    _detailPages[key] = url;

            },
            getDetailsPage: function (entity, id) {
                var url = _detailPages[entity.toLowerCase()];
                if (url)
                    return url.replace(':id', id);
            },
            start: start
        }
    });