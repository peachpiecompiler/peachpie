<?php
namespace classes\object_creation;

class Class1
{
	public function __construct()
	{
		echo 2;
	}
	
	public function Class1()
	{
		echo 3;
	}
};

class Class2 extends Class1
{
	public function __construct()
	{
		echo 4;
	}
	
	public function Class2()
	{
		echo 5;
	}

	public function Class1()
	{
		echo 6;
	}
};
	
echo (new Class1 instanceof Class1);
echo (new Class2 instanceof Class1);
