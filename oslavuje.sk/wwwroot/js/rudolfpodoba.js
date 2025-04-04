$(document).ready(function () {    
    RudolfPodobaMojeHesloFormApi();
});

function RudolfPodobaMojeHesloFormApi() {
    if ($('.api-password-group').length > 0) {
        $.ajax('/umbraco/surface/MojeHesloFormSurface/MojeHesloFormApiKey',
            {
                type: 'POST',
                contentType: 'application/json',
                dataType: 'json',
                success: function (data) {
                    $('.api-password-group #MojeHeslo').val(data.mainKey);
                    $('.api-password-group #PotvrdMojeHeslo').val(data.subKey);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    console.error("Chyba pri volaní API: " + textStatus + ", " + errorThrown);
                }
            });
    }
}
