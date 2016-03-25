/*
 * DREDA
 *
 */

// globals
var scene, camera, renderer, controls, stats;
var canvas = document.getElementById('data-canvas');

// custom globals for data
var data;
var organized = [], objects = [];
var grouped;
var groupedSize;
var groupedKeys;

// color options (rainbow husl, 30 shades)
colors = _.shuffle(
["#f79987",
 "#f7818a",
 "#f85d8c",
 "#ed3694",
 "#da369d",
 "#cb36a4",
 "#bc36aa",
 "#ac36af",
 "#9a36b3",
 "#8136b9",
 "#5735bf",
 "#3764c0",
 "#3884be",
 "#3997bd",
 "#3aa6bc",
 "#3ab1bb",
 "#3bbcba",
 "#3cc8b8",
 "#3dd5b7",
 "#3ee6b4",
 "#59f6af",
 "#86f5a8",
 "#a3f5a0",
 "#bbf598",
 "#d2f58e",
 "#eaf580",
 "#f6e67b",
 "#f6d27f",
 "#f6bf82",
 "#f7ae85"]);

// ratios
var edge = 500;
var dist = 500;
var pointToEdgeRatio = .1;
var edgePaddingMultiplier = 1.3;
var lineToEdgeRatio = .5;
var lineGapToEdgeRatio = .02;
var lineDashToEdgeRatio = .06;

// camera ratios
var cameraYToEdgeRatio = 1.1;
var cameraZToEdgeRatio = 1.1;
var cameraXToEdgeRatio = -1.1;

// cluster selection
var raycaster = new THREE.Raycaster();
var outlineCube;
var selectionCube;
var mouse = new THREE.Vector2();
var initMouseDown;
var finalMouseDown;
var mouseDown = 0;
var selectionPlaneXY, selectionPlaneXZ, selectionPlaneYZ;
var firstTimeSelection = true;
var selecting = false;
var pointCloud;

// calls
init();
animate();
resizeCamera();

////////////////
// functions //
////////////// 

function init() {
    $("#body3").mousedown = function () { };

    // get canvas
    canvas.width = canvas.clientWidth;
    canvas.height = canvas.clientHeight;

    // create a new scene
    scene = new THREE.Scene();

    // add a camera
    var viewAngle = 45;
    var aspect = canvas.clientWidth / canvas.clientHeight;
    var near = 0.1;
    var far = edge * 20;
    camera = new THREE.PerspectiveCamera(viewAngle, aspect, near, far);
    scene.add(camera);

    // inital camera position is relativized to the overall scale
    camera.position.set(
      edge * cameraXToEdgeRatio * 1.5,
      edge * cameraYToEdgeRatio * 1.5,
      edge * cameraZToEdgeRatio * 1.5);
    camera.lookAt(scene.position);

    // create the renderer
    // ReSharper disable once InconsistentNaming
    renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, canvas: canvas });
    renderer.setViewport(0, 0, canvas.clientWidth - canvas.clientWidth / 4, canvas.clientHeight);
    renderer.setClearColor(0x1A1A1A, 1);

    // controls
    controls = new THREE.OrbitControls(camera, document, renderer.domElement);
    controls.autoRotate = false;

    // stats
    // TODO: Add Stats

    // add lights
    var light = new THREE.DirectionalLight(0xffffff, 1.5);
    light.position.set(100, 300, 100);
    scene.add(light);

    // add grid
    var gridxz = new THREE.GridHelper(edge, edge * .1);          //size, step
    gridxz.setColors(
      new THREE.Color(0x323232),                              //chill green
      new THREE.Color(0x323232)
    );
    gridxz.position.set(0, 0, 0);
    scene.add(gridxz);

    // add x axis line
    var lineGeometry = new THREE.Geometry();
    var vertArray = lineGeometry.vertices;
    vertArray.push(new THREE.Vector3(-edge, 0, 0), new THREE.Vector3(edge, 0, 0));
    lineGeometry.computeLineDistances();
    var lineMaterial = new THREE.LineDashedMaterial({ color: 0xf68870, lineWidth: edge * lineToEdgeRatio, gapSize: lineGapToEdgeRatio * edge, dashSize: lineDashToEdgeRatio * edge });
    var line = new THREE.Line(lineGeometry, lineMaterial);
    scene.add(line);

    // add y axis line
    lineGeometry = new THREE.Geometry();
    vertArray = lineGeometry.vertices;
    vertArray.push(new THREE.Vector3(0, -edge, 0), new THREE.Vector3(0, edge, 0));
    lineGeometry.computeLineDistances();
    lineMaterial = new THREE.LineDashedMaterial({ color: 0x325cb1, lineWidth: edge * lineToEdgeRatio, gapSize: lineGapToEdgeRatio * edge, dashSize: lineDashToEdgeRatio * edge });
    line = new THREE.Line(lineGeometry, lineMaterial);
    scene.add(line);

    // add z axis line
    lineGeometry = new THREE.Geometry();
    vertArray = lineGeometry.vertices;
    vertArray.push(new THREE.Vector3(0, 0, -edge), new THREE.Vector3(0, 0, edge));
    lineGeometry.computeLineDistances();
    lineMaterial = new THREE.LineDashedMaterial({ color: 0x3beca3, lineWidth: edge * lineToEdgeRatio, gapSize: lineGapToEdgeRatio * edge, dashSize: lineDashToEdgeRatio * edge });
    line = new THREE.Line(lineGeometry, lineMaterial);
    scene.add(line);

    dist = camera.position.clone().distanceTo(new THREE.Vector3(0, 0, 0));

    // add the selection planes to be on the sides of the selection cube
    selectionPlaneXY = new THREE.Mesh(new THREE.PlaneGeometry(1, 1, 1, 1), new THREE.MeshBasicMaterial({ color: 'red', transparent: true, opacity: 0.5, side: THREE.DoubleSide, alphaTest: 0.5 }));
    selectionPlaneXZ = new THREE.Mesh(new THREE.PlaneGeometry(1, 1, 1, 1), new THREE.MeshBasicMaterial({ color: 'green', transparent: true, opacity: 0.5, side: THREE.DoubleSide, alphaTest: 0.5 }));
    selectionPlaneYZ = new THREE.Mesh(new THREE.PlaneGeometry(1, 1, 1, 1), new THREE.MeshBasicMaterial({ color: 'blue', transparent: true, opacity: 0.5, side: THREE.DoubleSide, alphaTest: 0.5 }));
    selectionPlaneXY.scale = 0.3;
    selectionPlaneXY.position.z = 1.05*edge;
    selectionPlaneXY.rotateX(0);
    selectionPlaneXZ.scale = 0.3;
    selectionPlaneXZ.position.y = 1.05*edge;
    selectionPlaneXZ.rotateX(Math.PI / 2);
    selectionPlaneYZ.scale = 0.3;
    selectionPlaneYZ.position.x = 1.05*edge;
    selectionPlaneYZ.rotateY(Math.PI / 2);
    scene.add(selectionPlaneXY);
    scene.add(selectionPlaneXZ);
    scene.add(selectionPlaneYZ);
    selectionPlaneXY.visible = false;
    selectionPlaneXZ.visible = false;
    selectionPlaneYZ.visible = false;

    selectionCube = new THREE.Mesh(new THREE.BoxGeometry(1, 1, 1, 20, 20 ,20), new THREE.MeshBasicMaterial({ color: 'red', opacity: 0.8 }));
    //scene.add(selectionCube);

    selectionCube.visible = false;
}


function animate() {
    requestAnimationFrame(animate);
    
    dist = camera.position.clone().distanceTo(new THREE.Vector3(0, 0, 0));

    render();
    update();
}

window.addEventListener("keydown", function (event) {
    var keyCode = event.keyCode;

    if (keyCode == 16) {
        selecting = true;
        controls.enabled = false;
    }
}, false);
window.addEventListener("keyup", function (event) {
    var keyCode = event.keyCode;

    // http://www.cambiaresearch.com/articles/15/javascript-char-codes-key-codes
    if (keyCode == 16) {
        selecting = false;
        controls.enabled = true;
    }
}, false);

var sizeX, sizeY, sizeZ;
var posX = 0, posY = 0, posZ = 0;
function update() {
    controls.update();

    // UPDATE
    if (mouseDown == 1) {
        if (finalMouseDown == undefined) return;


        if (initMouseDown.normal.z == 1 || initMouseDown.normal.z == -1) {
            sizeX = finalMouseDown.x - initMouseDown.x;
            sizeY = finalMouseDown.y - initMouseDown.y;

            selectionPlaneXY.scale.x = Math.abs(sizeX);
            selectionPlaneXY.scale.y = Math.abs(sizeY);

            posX = initMouseDown.x + sizeX / 2;
            posY = initMouseDown.y + sizeY / 2;

            selectionPlaneXY.position.z = initMouseDown.z + 0.0001 * initMouseDown.normal.z;
        }
        else if (initMouseDown.normal.y == 1 || initMouseDown.normal.y == -1) {
            sizeX = finalMouseDown.x - initMouseDown.x;
            sizeZ = finalMouseDown.z - initMouseDown.z;

            selectionPlaneXZ.scale.x = Math.abs(sizeX);
            selectionPlaneXZ.scale.y = Math.abs(sizeZ);

            posX = initMouseDown.x + sizeX / 2;
            posZ = initMouseDown.z + sizeZ / 2;

            selectionPlaneXZ.position.y = initMouseDown.y + 0.0001 * initMouseDown.normal.y;
        }
        else if (initMouseDown.normal.x == 1 || initMouseDown.normal.x == -1) {
            sizeY = finalMouseDown.y - initMouseDown.y;
            sizeZ = finalMouseDown.z - initMouseDown.z;

            selectionPlaneYZ.scale.x = Math.abs(sizeZ);
            selectionPlaneYZ.scale.y = Math.abs(sizeY);

            posY = initMouseDown.y + sizeY / 2;
            posZ = initMouseDown.z + sizeZ / 2;

            selectionPlaneYZ.position.x = initMouseDown.x + 0.0001 * initMouseDown.normal.x;
        }

        selectionPlaneXY.scale.x = Math.abs(sizeX);
        selectionPlaneXY.scale.y = Math.abs(sizeY);

        selectionPlaneXY.position.x = posX;
        selectionPlaneXY.position.y = posY;

        selectionPlaneXZ.scale.x = Math.abs(sizeX);
        selectionPlaneXZ.scale.y = Math.abs(sizeZ);

        selectionPlaneXZ.position.x = posX;
        selectionPlaneXZ.position.z = posZ;

        selectionPlaneYZ.scale.x = Math.abs(sizeZ);
        selectionPlaneYZ.scale.y = Math.abs(sizeY);

        selectionPlaneYZ.position.y = posY;
        selectionPlaneYZ.position.z = posZ;

        // Selection Cube
        selectionCube.scale.x = sizeX;
        selectionCube.scale.y = sizeY;
        selectionCube.scale.z = sizeZ;

        if (sizeX < 0) selectionCube.scale.x = -selectionCube.scale.x;
        if (sizeY < 0) selectionCube.scale.y = -selectionCube.scale.y;
        if (sizeZ < 0) selectionCube.scale.z = -selectionCube.scale.z;

        selectionCube.position.x = posX;
        selectionCube.position.y = posY;
        selectionCube.position.z = posZ;

        if (Math.abs(initMouseDown.normal.z) == 1) selectionPlaneXY.position.z = 1.05 * edge * initMouseDown.normal.z;
        if (Math.abs(initMouseDown.normal.x) == 1) selectionPlaneYZ.position.x = 1.05 * edge * initMouseDown.normal.x;
        if (Math.abs(initMouseDown.normal.y) == 1) selectionPlaneXZ.position.y = 1.05 * edge * initMouseDown.normal.y;

    }


    //stats.update();
}

function render() {
    renderer.render(scene, camera);
}

// resize - listener

window.addEventListener("resize", resizeCamera);

function resizeCamera()
{
    canvas.width = canvas.clientWidth;
    canvas.height = canvas.clientHeight;
    renderer.setViewport(0, 0, canvas.clientWidth, canvas.clientHeight);
    camera.aspect = canvas.clientWidth / canvas.clientHeight;
    camera.updateProjectionMatrix();
}

// mouse down
canvas.addEventListener("mousedown", function ( event ) 
{
    if (!(selecting === true)) return;

    // calculate mouse position in normalized device coordinates
    // (-1 to +1) for both components
    var evLay = getOffset(event);
    mouse.x =   (evLay.x / canvas.offsetWidth) * 2 - 1;
    mouse.y = -(evLay.y / canvas.offsetHeight) * 2 + 1;

    // update the picking ray with the camera and mouse position	
    raycaster.setFromCamera(mouse, camera);

    // calculate objects intersecting the picking ray
    var intersects = raycaster.intersectObjects([outlineCube], true);

    if (intersects.length > 0) {
        var normal;
        for (var i = 0; i < intersects.length; i++) {
            initMouseDown = intersects[i].point.clone();
            initMouseDown.normal = normal = intersects[i].face.normal;
            break;
        }

        mouseDown = 1;
    }
});

function getOffset(evt) {
    var el = evt.target,
        x = 0,
        y = 0;

    while (el && !isNaN(el.offsetLeft) && !isNaN(el.offsetTop)) {
        x += el.offsetLeft - el.scrollLeft;
        y += el.offsetTop - el.scrollTop;
        el = el.offsetParent;
    }

    x = evt.clientX - x;
    y = evt.clientY - y;

    return { x: x, y: y };
}

// mouse up
canvas.addEventListener("mousemove", function (event)
{
    if( mouseDown == 1 ) {
        selectionPlaneXY.visible = true;
        selectionPlaneXZ.visible = true;
        selectionPlaneYZ.visible = true;
        selectionCube.visible = true;

        // calculate mouse position in normalized device coordinates
        // (-1 to +1) for both components
        var evLay = getOffset(event);
        mouse.x =  (evLay.x / canvas.offsetWidth ) * 2 - 1;
        mouse.y = -(evLay.y / canvas.offsetHeight) * 2 + 1;

        //console.log( "X " + evLay.x + "Y");

        // update the picking ray with the camera and mouse position	
        raycaster.setFromCamera(mouse, camera);

        // calculate objects intersecting the picking ray
        var intersects = raycaster.intersectObjects([outlineCube]);

        if (intersects.length > 0) {
            var normal;
            for (var i = 0; i < intersects.length; i++) {
                finalMouseDown = intersects[i].point;
                break;
            }
        }
    }
});

// mouse up
window.addEventListener("mouseup", function (event)
{
    mouseDown = 0;
    if (selecting === true) {
        firstTimeSelection = false;
        selecting = false;

        colorSelectedPoints(window.selectedColor);
    }
});

function rotateToggle() {
    controls.autoRotate = !controls.autoRotate;
}

function resetCamera() {
    camera.position.set(
      edge * cameraXToEdgeRatio,
      edge * cameraYToEdgeRatio,
      edge * cameraZToEdgeRatio);
    camera.lookAt(scene.position);
}

////////
////////
// DATA PROCESSING
///////
///////

function loadData() {

    // clear container
    organized = [];

    var dataLength = _.size(_.values(data)[0]);

    for (var i = 0; i < dataLength; i++) {
        var point = {};
        for (p in data) {
            if (data.hasOwnProperty(p)) {
                point[p] = data[p][i];
            }
        }
        organized.push(point);
    }
}

function plotData() {

    if (data == undefined) return false;

    // get those points

    loadData();

    // resize edge based on max min & reinitialize planes
    edge = getLimits() * edgePaddingMultiplier;

    // set up the initial sizes of the selection planes
    sizeX = edge * 2; sizeY = edge * 2; sizeZ = edge * 2;

    centroid = getCentroid();

    init();

    var sprite = THREE.ImageUtils.loadTexture("../Images/disc.png");

    console.log("Initalized");
    // zip and group

    var keys = Object.keys(data);

    grouped = _.groupBy(organized, keys[3]); // group by color
    groupedSize = _.size(grouped);
    groupedKeys = Object.keys(grouped);

    for (var i = 0; i < groupedSize; i++) {

        // create new point cloud material
        var pointGeometry = new THREE.BufferGeometry();

        var pointColors = new Float32Array(grouped[groupedKeys[i]].length * 3); // r g b colors
        var vertices = new Float32Array(grouped[groupedKeys[i]].length * 3); // three components per vertex

        // loop through points
        for (var j = 0; j < grouped[groupedKeys[i]].length; j++) {

            var vertex = new THREE.Vector3();
            vertex.x = grouped[groupedKeys[i]][j][keys[0]] - centroid.x;
            vertex.y = grouped[groupedKeys[i]][j][keys[1]] - centroid.y;
            vertex.z = grouped[groupedKeys[i]][j][keys[2]] - centroid.z;
            vertices[j * 3 + 0] = vertex.x;
            vertices[j * 3 + 1] = vertex.y;
            vertices[j * 3 + 2] = vertex.z;

            // put the
            // assign colors
            pointColors[j * 3 + 0] = 1.0;
            pointColors[j * 3 + 1] = 1.0;
            pointColors[j * 3 + 2] = 1.0;
        }

        // add position and color
        pointGeometry.addAttribute('position', new THREE.BufferAttribute(vertices, 3));
        pointGeometry.addAttribute('color', new THREE.BufferAttribute(pointColors, 3));

        // add an attribute
        var numVertices = pointGeometry.attributes.position.count;
        var alphas = new Float32Array(numVertices * 1); // 1 values per vertex

        for (var j = 0; j < numVertices; j++) {
            alphas[j] = 0.5;
        }

        pointGeometry.addAttribute('alpha', new THREE.BufferAttribute(alphas, 1));

        console.log(grouped[groupedKeys[i]].length + " points in cluster " + groupedKeys[i]);

        // point cloud material
        var shaderMaterial = new THREE.ShaderMaterial({
            uniforms: {
                shadedCircle: { type: 't', value: sprite },
                windowWidth: { type: 'f', value: canvas.width },
                windowHeight: { type: 'f', value: canvas.height }
            },
            vertexShader: document.getElementById('vertexshader').textContent,
            fragmentShader: document.getElementById('fragmentshader').textContent,
            transparent: true

        });

        console.log(canvas.width + " " + canvas.height);

        // create new point cloud
        var pointCloudMaterial = new THREE.PointsMaterial({
            size: edge * pointToEdgeRatio,
            //color: colors[i],
            vertexColors: THREE.VertexColors,
            alphaTest: 0.5,
            transparent: true,
            opacity: 0.8,
            map: sprite
        });

        pointCloud = new THREE.Points(pointGeometry, shaderMaterial);
        scene.add(pointCloud);
        console.log(colors[i]);
    }

    //add cube
    outlineCube = cube(edge * 2);

    //scene.add(outlineCube);

    resizeCamera();

    return true;
}

function colorSelectedPoints(color) {
    for (var i = 0; i < pointCloud.geometry.attributes.position.count; i++) {
        if (inBoundsOfSelection(new THREE.Vector3(pointCloud.geometry.attributes.position.array[i * 3], pointCloud.geometry.attributes.position.array[i * 3 + 1], pointCloud.geometry.attributes.position.array[i * 3 + 2])))
        {
            // assign colors
            pointCloud.geometry.attributes.color.array[i * 3 + 0] = new THREE.Color(color).r;
            pointCloud.geometry.attributes.color.array[i * 3 + 1] = new THREE.Color(color).g;
            pointCloud.geometry.attributes.color.array[i * 3 + 2] = new THREE.Color(color).b;
            pointCloud.geometry.attributes.alpha.array[i] = 1.0;
        }
        else
        {
            // assign colors
            //pointCloud.geometry.attributes.color.array[i * 3 + 0] = new THREE.Color(color).r;
            //pointCloud.geometry.attributes.color.array[i * 3 + 1] = new THREE.Color(color).g;
            //pointCloud.geometry.attributes.color.array[i * 3 + 2] = new THREE.Color(color).b;
            pointCloud.geometry.attributes.alpha.array[i] = 0.1;
        }
    }
    pointCloud.geometry.attributes.color.needsUpdate = true;
    pointCloud.geometry.attributes.alpha.needsUpdate = true;
}


function inBoundsOfSelection( vertex )
{
    //console.log("NONE X" + (selectionCube.position.x + selectionCube.scale.x / 2) + " " + (selectionCube.position.x - selectionCube.scale.x / 2) + " " + vertex.x);
    //console.log("NONE Y" + (selectionCube.position.y + selectionCube.scale.y / 2) + " " + (selectionCube.position.y - selectionCube.scale.y / 2) + " " + vertex.y);
    //console.log("NONE Z" + (selectionCube.position.z + selectionCube.scale.z / 2) + " " + (selectionCube.position.z - selectionCube.scale.z / 2) + " " + vertex.z);
    if (vertex.x < selectionCube.position.x + selectionCube.scale.x / 2
        && vertex.x > selectionCube.position.x - selectionCube.scale.x / 2
        && vertex.y < selectionCube.position.y + selectionCube.scale.y / 2
        && vertex.y > selectionCube.position.y - selectionCube.scale.y / 2
        && vertex.z < selectionCube.position.z + selectionCube.scale.z / 2
        && vertex.z > selectionCube.position.z - selectionCube.scale.z / 2)
    {
        //console.log("WHITE " + selectionCube.position.x + selectionCube.scale.x / 2 + " " + vertex.x);
        return true;
    }
    else return false;
}

function getLimits() {
    // container for potential x-z grid size
    var potentialEdges = [];
    for (col in data) {
        if (data.hasOwnProperty(col)) {
            // http://stackoverflow.com/questions/11142884
            var min = _.min(data[col]);
            var max = _.max(data[col]);
            if (isFinite(min) && isFinite(max)) {
                potentialEdges.push( (max - min) / 2 );
            }
        }
    }
    return _.max(potentialEdges);
}

function getCentroid() {
    // container for potential x-z grid size
    var potentialEdges = [];

    var vertexCentroid = new THREE.Vector3();

    count = 0;
    centroid = [];
    for (col in data) {
        if (data.hasOwnProperty(col)) {
            // http://stackoverflow.com/questions/11142884
            var min = _.min(data[col]);
            var max = _.max(data[col]);
        }
        centroid[count] = (min + max) / 2;
        count++;
    }

    vertexCentroid.x = centroid[0];
    vertexCentroid.y = centroid[1];
    vertexCentroid.z = centroid[2];

    return vertexCentroid;
}

function cube(size) {

    var cubeText = THREE.ImageUtils.loadTexture('images/threejsimages/cubeOutline.png');
    var cube = new THREE.Mesh(new THREE.CubeGeometry(size, size, size), new THREE.MeshBasicMaterial({ map: cubeText, transparent: true, opacity: 0.5 }));

    return cube;
}

