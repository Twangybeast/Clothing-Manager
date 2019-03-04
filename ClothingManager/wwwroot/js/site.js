// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
let countTableExpanded = $(".extendable-table>tbody").children().length - 1;
function onExpandTableMore() {
    let row = $(".extendable-table>tbody>.d-none").clone();
    row.appendTo(".extendable-table");
    row.removeClass("d-none");
    countTableExpanded++;
    let i = countTableExpanded;
    row.find(".form-control").each(function (a, e) {
        e.setAttribute("name", e.getAttribute("name") + i);
        e.setAttribute("id", e.getAttribute("id") + i);
    });
    row.find("label").each(function (a, e) {
        e.setAttribute("for", e.getAttribute("for") + i);
    });
    
    setEditableSelect();
} 


function positionGradientField(useYNotZ) {
    $(".gradient-item").each(function (index, item) {
        let x = this.dataset.positionX;
        let y;
        if (useYNotZ) {
            y = this.dataset.positionY;
        } else {
            y = this.dataset.positionZ;
        }
        x = x * scaleGradient;
        y = y * scaleGradient;
        item.style.left = x + "px";
        item.style.top = y + "px";
    });
}

(function () {
    let table = $(".expand-table")[0];
    if (table != undefined) {
        table.addEventListener("click", onExpandTableMore);
        onExpandTableMore();
    }
    addAddOutfitClothingListeners();
    //setEditableSelect();
})();
function addAddOutfitClothingListeners() {
    $('.add-outfit-clothing').each(function () {
        this.addEventListener('click', function () {
            $.ajax({
                url: '/Outfits/AddClothing',
                method: 'post',
                data: { clothingId: this.getAttribute('data-clothing-id') },
                success: function (data) {
                    //TODO 
                }
            });
        });
    });
}
function addRemoveOutfitClothingListeners() {

    $('.remove-outfit-clothing').each(function () {
        this.addEventListener('click', function () {
            $.ajax({
                url: '/Outfits/RemoveClothing',
                method: 'post',
                data: { clothingId: this.getAttribute('data-clothing-id') },
                success: function (data) {
                    updateCurrentOutfitData();
                    //TODO 
                }
            });
        });
    });
}

function addOutfitOrderListeners() {
    $('.outfit-clothing-order-up').each(function () {
        this.addEventListener('click', function () {
            $.ajax({
                url: '/Outfits/ClothingOrderUp',
                method: 'post',
                data: { clothingId: this.getAttribute('data-clothing-id') },
                success: function (data) {
                    updateCurrentOutfitData();
                    //TODO 
                }
            });
        });
    });

    $('.outfit-clothing-order-down').each(function () {
        this.addEventListener('click', function () {
            $.ajax({
                url: '/Outfits/ClothingOrderDown',
                method: 'post',
                data: { clothingId: this.getAttribute('data-clothing-id') },
                success: function (data) {
                    updateCurrentOutfitData();
                    //TODO 
                }
            });
        });
    });
}

function setEditableSelect() {
    /*
    $('.editable-select').each(function () {
        $(this).editableSelect();
    });*/
    $('.editable-select').each(function () {
        if (this.id != "tagkey" && this.id != "tagvalue") {
            $(this).editableSelect();
        }
    });


    $('.select-tag-values').each(function (index, a) {
        let select = $(a);
        console.log('ready')
        select.on('show.editable-select', function (e) {
            console.log('selected')
            let number = this.id.substr("tagvalue".length);
            let tagKey = $('#tagkey' + number).val();
            $.ajax({
                dataType: 'json',
                url: '/Tags/GetTagValues',
                data: { tag: tagKey },
                async: false,
                success: function (data) {
                    select.editableSelect('clear');
                    for (let i = 0; i < data.length; i++) {
                        select.editableSelect('add', data[i]);
                    }
                }
            });
        });
    });
        
}

function centerGradientField() {
    let viewport = $('.gradient-field-viewport');
    let field = $('.gradient-field');
    viewport.scrollLeft((field.width() - viewport.width()) / 2);
    viewport.scrollTop((field.height() - viewport.height()) / 2);
}

function editOutfit(id) {
    document.cookie = 'currentOutfitId=' + id + ';path=/;';
}

function toggleFullScreen() {
    var doc = window.document;
    var docEl = doc.documentElement;

    var requestFullScreen = docEl.requestFullscreen || docEl.mozRequestFullScreen || docEl.webkitRequestFullScreen || docEl.msRequestFullscreen;
    var cancelFullScreen = doc.exitFullscreen || doc.mozCancelFullScreen || doc.webkitExitFullscreen || doc.msExitFullscreen;

    if (!doc.fullscreenElement && !doc.mozFullScreenElement && !doc.webkitFullscreenElement && !doc.msFullscreenElement) {
        requestFullScreen.call(docEl);
    }
    else {
        cancelFullScreen.call(doc);
    }
}
