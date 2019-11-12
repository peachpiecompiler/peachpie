<?php

namespace image\imageinfo;

function test() {
    $fname = "round-socialmedia-68x68.png";
    $type = exif_imagetype($fname);
    print_r($type);

    $info = getimagesize($fname, $more);
    print_r($info);
    print_r($more);
}

test();
