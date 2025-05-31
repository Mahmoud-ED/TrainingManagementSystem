$(document).ready(function () {
    // تحميل الصفحة عبر AJAX
    function loadPage(url, pushState = true) {
        $("#loader-overlay").fadeIn(); // إظهار اللودر عند بدء التحميل

        $.get(url, function (data) {
            const newContent = $(data).find('.main-panel').html();
            $('.main-panel').html(newContent);

            // تحديث عنوان الصفحة (URL) إذا تم التغيير
            if (pushState) {
                window.history.pushState({ path: url }, '', url);
            }

            // إعادة تهيئة DataTables والسكربتات
            initializeDataTables();
            executePageScripts(data);

            // إعادة تفعيل الأحداث للروابط الجديدة
            attachAjaxLinks();
        }).always(function () {
            $("#loader-overlay").fadeOut(); // إخفاء اللودر بعد انتهاء التحميل
        });
    }

    // تفعيل الروابط التي تستخدم AJAX
    function attachAjaxLinks() {
        $('.ajax-link').off('click').on('click', function (e) {
            e.preventDefault();
            const url = $(this).attr('href');
            loadPage(url);
        });
    }

    // دعم التنقل باستخدام Back و Forward
    window.onpopstate = function () {
        loadPage(location.href, false);
    };

    // إعادة تهيئة DataTables
    function initializeDataTables() {
        if ($.fn.DataTable) {
            $('.dataTable').each(function () {
                if ($.fn.DataTable.isDataTable(this)) {
                    $(this).DataTable().destroy();
                }
                new DataTable(this, {
                    processing: true,       // تفعيل مؤشر المعالجة
                    processing: '<i class="fa fa-spinner fa-spin fa-2x"></i> جاري التحميل...'
                    //language: {
                    //    url: '//cdn.datatables.net/plug-ins/2.0.3/i18n/ar.json'
                    //}
                    //, scrollX: true
                });
            });
        }
    }

    // تنفيذ السكربتات من الصفحة
    function executePageScripts(data) {
        const scripts = $(data).find('script');
        scripts.each(function () {
            try {
                eval($(this).text());
            } catch (error) {
                console.error('خطأ في تنفيذ السكربت:', error);
            }
        });
    }

    // تفعيل الأحداث عند التحميل الأول
    attachAjaxLinks();
});
