<?php

interface ILudolf
{
	const pi = 3.14;
};

interface IEuler
{
	const e = 2.71;
};

class Math implements ILudolf, IEuler
{
};

echo ILudolf::pi;
echo ' ';
echo IEuler::e;
echo ' ';
echo Math::pi;
echo ' ';
echo Math::e;
