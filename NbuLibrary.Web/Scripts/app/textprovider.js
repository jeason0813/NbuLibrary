/// <reference path="../lib/RequireJS/require.debug.js" />

//module that is responsible for providing text resources - labels, messages and etc.
define('textprovider',
    ['jquery'],
    function ($) {        

        var _data = {};
        $(NbuLib.embed.texts).each(function () {
            _data[this.Key] = this.Value;
        });

        var loaded = false;

        function get(key) {
            return _data[key] ? _data[key] : key;
        };

        function set(key, val) {
            _data[key] = val;
            return $.post('/api/Text/SetText', { Key: key, Value: val });
        };

        function unset(key) {
            _data[key] = null;
            return $.post('/api/Text/UnsetText', { Key: key, Value : ''});
        };


        //interface
        return {
            get: get,
            set: set,
            unset: unset,
            all: _data
        };
    });