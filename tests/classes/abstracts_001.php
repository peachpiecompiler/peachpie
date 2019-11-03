<?php
namespace classes\abstracts_001;

interface LogEntry {

	function a();
    function b();
    function c();
    function d();
    function e();
    function xxx();
}

abstract class LogEntryBase implements LogEntry {

    // note: missing abstract declarations of a,b,c,d,e causes System.TypeLoadException

	function xxx() { echo __METHOD__, "\n"; }
}

class ManualLogEntry extends LogEntryBase {

    function a() {
	}

    function b() {
	}

	function c() {
	}

	function d() {
	}

	function e() {
        echo __METHOD__, "\n";
	}
}

// test everything gets called correctly:

function test1(LogEntryBase $x)
{
    $x->e();
    $x->xxx();
}

function test2(LogEntry $x)
{
    $x->e();
    $x->xxx();
}

(new ManualLogEntry)->xxx();
test1(new ManualLogEntry);
test2(new ManualLogEntry);
