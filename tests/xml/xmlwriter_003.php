<?php
namespace xml\xmlwriter_003;

function normalize($xml) {
  return trim(str_replace(array("UTF-8", " />"), array("utf-8", "/>"), $xml));
}

function test() {
    $xw = new \XMLWriter();
    $xw->openMemory();
    $xw->startDocument('1.0', 'utf-8');

    // Tests comments and cdata and pi sections

    $xw->startElement("tag1");
    $xw->startAttribute('attr2');
    $xw->text("attr2_value");
    $xw->endAttribute();

    // Comments
    $xw->startComment();
    $xw->endComment();

    $xw->startComment();
    $xw->text("Characters : < , > ");
    $xw->endComment();

    $xw->startComment();
    $xw->startDtdElement("el");
    $xw->endComment();

    $xw->startComment();
    $xw->startElement("tag2");
    $xw->text("Characters : < , > ");
    $xw->endElement();
    $xw->endComment();

    $xw->startComment();
    $xw->startComment();
    $xw->startElement("tag3");
    $xw->startAttribute('attr2');
    $xw->text("Characters : < , > ");
    $xw->endAttribute();
    $xw->text("Characters : < , > ");
    $xw->endElement();
    $xw->endComment();
    $xw->endComment();

    $xw->startComment();
    $xw->text("Characters : < , > ");
    $xw->startCdata();
    $xw->text("Characters : < , > ");
    $xw->endCdata();
    $xw->text("Characters : < , > ");
    $xw->endComment();

    $xw->startComment();
    $xw->text("Characters : < , > ");
    $xw->startPi("php");
    $xw->text("echo 1 < 0;");
    $xw->endPi();
    $xw->text("Characters : < , > ");
    $xw->endComment();

    $xw->startComment();
    $xw->text("Characters : < , > ");
    $xw->startComment();
    $xw->text("Characters : < , > ");
    $xw->endComment();
    $xw->text("Characters : < , > ");
    $xw->endComment();

    $xw->startElement("tag");
    $xw->startComment();
    $xw->endElement();
    $xw->endComment();

    $xw->startElement("tag");
    $xw->startComment();
    $xw->writeAttribute("attr","1");
    $xw->endComment();
    $xw->endElement();

    //TODO PI
    $xw->startComment();
    $xw->startPi("php");
    $xw->text("echo pi in comment;");
    $xw->endPi();
    $xw->endComment();

    $xw->startPi("php");
    $xw->text("1 > 0");
    $xw->endPi();

    $xw->startPi("php");
    $xw->startComment();
    $xw->text("echo pi in comment;");
    $xw->endComment();
    $xw->endPi();

    // CDATA 

    $xw->startCdata();
    $xw->text("some text");
    $xw->endCdata();

    $xw->startCdata();
    $xw->text("1 > 0");
    $xw->endCdata();

    $xw->startCdata();
    $xw->startElement("tag2");
    $xw->text("some text");
    $xw->endElement();
    $xw->endCdata();

    $xw->startCdata();
    $xw->startCdata();
    $xw->startElement("tag3");
    $xw->startAttribute('attr2');
    $xw->text("attr2_value");
    $xw->endAttribute();
    $xw->text("Comment in comment");
    $xw->endElement();
    $xw->endCdata();
    $xw->endCdata();

    $xw->startCdata();
    $xw->startComment();
    $xw->text("cdata in comment");
    $xw->endComment();

    $xw->endDocument();

    // Force to write and empty the buffer
    echo normalize($xw->flush(true));
}

test();
