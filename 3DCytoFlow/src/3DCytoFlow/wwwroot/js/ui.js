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

     //   $("#loader").removeClass("ui active dimmer");

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
    $("#clusterSize").append(0);
    $("#info-box").removeClass("hidden");

    // cluster info
    $("#clusterInfo").empty();
//    for (var i = 0; i < groupedSize; i++) {
//        $("#clusterInfo").append(
//          "<div style=\"color:" + colors[i] + ";\">" + groupedKeys[i] + " : " + grouped[groupedKeys[i]].length + "</div>"
//          );
    //    }

    //add the clusters
    window.getClusters();
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
///fill the color selector with the array of colors
function fillColorSelector() {
    $("#clusterInput").after("<select class=\"form-control\" id=\"colorselector\" style=\"margin: 10px;\"></select>");

    for (var i = 0; i < colors.length; i++) {
        $("#colorselector").append("<option value=\"" + i + "\" data-color=\"" + colors[i].toUpperCase() + "\"></option>");
    }

    $("#colorselector").colorselector({
        callback: function (value, color, title) {
            window.selectedColor = color;
            window.colorSelectedPoints(window.selectedColor, 1);
        }
    });
}

//selects and deselects clusters
function selectDeselectClusters(id, e) {

    //get the data of the cluster that its being checked
    var i;
    for (i = 0; i < window.currentClusters.length; i++) {
        if (window.currentClusters[i].Id === parseInt(id)) break;
    }

    //set the values of the selectionCube and change accordingly
    var split = window.currentClusters[i].Name.split("#");

    var color = !window.currentClusters[i].Name.includes("#") ? "#" + window.currentClusters[i].Color : "#" + split[1];

    var height = window.currentClusters[i].Height;
    var depth = window.currentClusters[i].Depth;
    var width = window.currentClusters[i].Width;
    var x = window.currentClusters[i].X;
    var y = window.currentClusters[i].Y;
    var z = window.currentClusters[i].Z;

    window.selectionCube.scale.x = depth;
    window.selectionCube.scale.y = width;
    window.selectionCube.scale.z = height;
    window.selectionCube.position.x = x;
    window.selectionCube.position.y = y;
    window.selectionCube.position.z = z;

    if (e != undefined && !$(e).is(":checked")) {
        //being unchecked. Apply transparency
        window.colorSelectedPoints(color, 3);
    } else if (e != undefined) {
        //being checked
        window.colorSelectedPoints(color, 0);
    } else {
        //being checked
        window.colorSelectedPoints(color, 0);
    }
}
