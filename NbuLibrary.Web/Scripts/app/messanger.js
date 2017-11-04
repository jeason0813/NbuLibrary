define('messanger',
    [$],
    function () {

        function info(msg){
            var n = noty({
                text: msg,
                layout: 'topRight',
                callback: {
                    afterShow: function () {
                        setTimeout(function () {
                            n.close();
                        }, 4000);
                    }
                }
            });
        };

        function success(msg)
        {
            var n = noty({
                text: msg,
                layout: 'topRight',
                type: 'success',
                callback: {
                    afterShow: function () {
                        setTimeout(function () {
                            n.close();
                        }, 4000);
                    }
                }
            });
        }

        function warning(msg) {
            var n = noty({
                text: msg,
                layout: 'topRight',
                type: 'warning',
                callback: {
                    afterShow: function () {
                        setTimeout(function () {
                            n.close();
                        }, 4000);
                    }
                }
            });
        }
        function error(msg) {
            noty({
                text: msg,
                layout: 'topRight',
                type: 'error'
            });
        }

        return {
            info: info,
            error: error,
            warning: warning,
            success : success
        };
    });