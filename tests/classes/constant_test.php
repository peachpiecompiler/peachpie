<?php
namespace classes\constant_test;

define("X", 3.14);

interface ILudolf
{
	const pi = 3.14;
};

interface IEuler
{
	const pi2 = ILudolf::pi; // compile time resolved constant value, produces CLR const
	const pi3 = X; // run time constant value, produces CLR instance field
	const e = 2.71;
};

class Math implements ILudolf, IEuler
{
};

echo ILudolf::pi, ' ';
echo IEuler::e, ' ';
echo Math::pi, ' ';
echo Math::e, ' ';
echo Math::pi2, ' ', Math::pi3, ' ';
