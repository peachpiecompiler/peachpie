<?php
namespace classes\overloading_007;

interface I {
	function rollback();
}

class X {
    function rollBack() {}	// different casing
}

class Y extends X implements I {

}

class Z extends Y {
	function RollBack() {} // different casing
}