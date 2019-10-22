<?php
namespace spl\RecursiveIteratorIterator_002; 
class X extends \RecursiveIteratorIterator
{
	function beginIteration()
	{
		echo __METHOD__ . "; ";
		return parent::beginIteration();
	}
	function endIteration()
	{
		echo __METHOD__ . "; ";
		return parent::endIteration();
	}
	
	function beginChildren()
	{
		echo __METHOD__ . "; ";
		return parent::beginChildren();
	}
	function endChildren()
	{
		echo __METHOD__ . "; ";
		return parent::endChildren();
	}
    function callHasChildren()
	{
		echo __METHOD__ . "; ";
		return parent::callHasChildren();
	}
	function callGetChildren()
	{
		echo __METHOD__ . "; ";
		return parent::callGetChildren();
	}
	
	function current()
	{
		echo __METHOD__ . "; ";
		return parent::current();
	}
	
	function next()
	{
		echo __METHOD__ . "; ";
		return parent::next();
	}
	
	//function valid()
	//{
	//	echo __METHOD__ . "; ";
	//	return parent::valid();
	//}
	
	function rewind()
	{
		echo __METHOD__ . "; ";
		return parent::rewind();
	}
	
	function nextElement()
	{
		echo __METHOD__ . "; ";
		return parent::nextElement();
	}
}
$arr = array(
    'Zero',
    'name'=>'Adil',
    'address' => array(
        'city'=>'Dubai',
        'tel' => array(
            'int' => 971,
            'tel'=>12345487)),
    '' => 'nothing');
print_r( iterator_to_array(new X(new \RecursiveArrayIterator($arr), \RecursiveIteratorIterator::LEAVES_ONLY, \RecursiveIteratorIterator::CATCH_GET_CHILD)) );
print_r( iterator_to_array(new X(new \RecursiveArrayIterator($arr), \RecursiveIteratorIterator::SELF_FIRST, \RecursiveIteratorIterator::CATCH_GET_CHILD)) );
print_r( iterator_to_array(new X(new \RecursiveArrayIterator($arr), \RecursiveIteratorIterator::CHILD_FIRST, \RecursiveIteratorIterator::CATCH_GET_CHILD)) );
