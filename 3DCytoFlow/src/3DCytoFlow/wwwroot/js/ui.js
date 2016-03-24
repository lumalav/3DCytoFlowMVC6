//// //// //// //// initalizations //// //// //// //// 
// info
$("#info-box").toggleClass("hidden");
//// //// //// //// file upload //// //// //// ////  

var userFile;
var data;

// file selector so we can hide ugly input
var fileSelector = $("#fileSelector");
var fileInput = $("#fileInput");


// These next two functions are unnecessary... however
// I feel that even the slight glimpse of the loader
// gives a polished look to the UI

function hideAndLoad() {
    $(".ui.modal").modal("hide");
    $(".ui.sidebar").sidebar("hide");
}

function plot() {

    setTimeout(function () {

        if (!plotData()) {
            alert("This file is corrupted");
            return;
        }

        $("#loader").removeClass("ui active dimmer");

        updateInfoBox();

    }, 200);

    hideAndLoad();
}

// popups

$("#side-explanation a").popup({ position: "right center" });


//// //// //// //// info box //// //// //// //// 
// aka glowing / pulsing effect
$(".info.icon")
  .transition("set looping")
  .transition("pulse", "1500ms");

$(".info.icon").click(function () {
    $("#info-box").toggleClass("hidden");
    $("#clusterBox").toggleClass("hidden");
});

function updateInfoBox() {
    // total size
    $("#statsSize").empty();
    $("#statsSize").append(_.size(organized));

    // number clusters
    $("#clusterSize").empty();
    $("#clusterSize").append(_.uniq(organized, Object.keys(data)[3]).length);
    $("#info-box").removeClass("hidden");

    // cluster info
    $("#clusterInfo").empty();
//    for (var i = 0; i < groupedSize; i++) {
//        $("#clusterInfo").append(
//          "<div style=\"color:" + colors[i] + ";\">" + groupedKeys[i] + " : " + grouped[groupedKeys[i]].length + "</div>"
//          );
//    }
}

//initalize rotateToggle
$(".ui.checkbox")
  .checkbox();

$(".ui.checkbox").click(function () {
    rotateToggle();
});

$(".ui.undo").click(function () {
    resetCamera();
});

//area select
/*
$(".pusher").mousedown(function (e) {

    $("#big-ghost").remove();
    $(".ghost-select").addClass("ghost-active");
    $(".ghost-select").css({
        'left': e.pageX,
        'top': e.pageY
    });

    initialW = e.pageX;
    initialH = e.pageY;

    $(document).bind("mouseup", selectElements);
    $(document).bind("mousemove", openSelector);

});*/


function openSelector(e) {
    var w = Math.abs(initialW - e.pageX);
    var h = Math.abs(initialH - e.pageY);

    $(".ghost-select").css({
        'width': w,
        'height': h
    });
    if (e.pageX <= initialW && e.pageY >= initialH) {
        $(".ghost-select").css({
            'left': e.pageX
        });
    } else if (e.pageY <= initialH && e.pageX >= initialW) {
        $(".ghost-select").css({
            'top': e.pageY
        });
    } else if (e.pageY < initialH && e.pageX < initialW) {
        $(".ghost-select").css({
            'left': e.pageX,
            "top": e.pageY
        });
    }
}

function selectElements(e) {
    $("#score>span").text('0');
    $(document).unbind("mousemove", openSelector);
    $(document).unbind("mouseup", selectElements);
    var maxX = 0;
    var minX = 5000;
    var maxY = 0;
    var minY = 5000;
    var totalElements = 0;
    var elementArr = new Array();
    $(".elements").each(function () {
        var aElem = $(".ghost-select");
        var bElem = $(this);
        var result = doObjectsCollide(aElem, bElem);

        console.log(result);
        if (result == true) {
            $("#score>span").text(Number($("#score>span").text()) + 1);
            var aElemPos = bElem.offset();
            var bElemPos = bElem.offset();
            var aW = bElem.width();
            var aH = bElem.height();
            var bW = bElem.width();
            var bH = bElem.height();

            var coords = checkMaxMinPos(aElemPos, bElemPos, aW, aH, bW, bH, maxX, minX, maxY, minY);
            maxX = coords.maxX;
            minX = coords.minX;
            maxY = coords.maxY;
            minY = coords.minY;
            var parent = bElem.parent();

            //console.log(aElem, bElem,maxX, minX, maxY,minY);
            if (bElem.css("left") === "auto" && bElem.css("top") === "auto") {
                bElem.css({
                    'left': parent.css('left'),
                    'top': parent.css('top')
                });
            }
            $("body").append("<div id='big-ghost' class='big-ghost' x='" + Number(minX - 20) + "' y='" + Number(minY - 10) + "'></div>");

            $("#big-ghost").css({
                'width': maxX + 40 - minX,
                'height': maxY + 20 - minY,
                'top': minY - 10,
                'left': minX - 20
            });


        }
    });

    $(".ghost-select").removeClass("ghost-active");
    $(".ghost-select").width(0).height(0);

    ////////////////////////////////////////////////

}
