<?php
namespace strings\sprintf2;

echo sprintf("%e\n", 1.123456789);
echo sprintf("%.10e\n", 1.123456789);
echo sprintf("%.0e\n", 1.123456789);
echo sprintf("%.1e\n", 1.123456789);
echo sprintf("%5.1e\n", 1.123456789);
echo "---\n";
echo sprintf("%f\n", 1.123456789);
echo sprintf("%.10f\n", 1.123456789);
echo sprintf("%.0f\n", 1.123456789);
echo sprintf("%.1f\n", 1.123456789);
echo sprintf("%5.1f\n", 1.123456789);
echo "---\n";
echo sprintf("%e\n", 123.123456789);
echo sprintf("%.10e\n", 123.123456789);
echo sprintf("%.0e\n", 123.123456789);
echo sprintf("%.1e\n", 123.123456789);
echo sprintf("%5.1e\n", 123.123456789);
echo "---\n";
echo sprintf("%12.10e\n", 1);
echo sprintf("%e\n", 111.234E-18);
echo sprintf("%e\n", 1.234E+18);
echo sprintf("%e\n", 9843243.12);
echo sprintf("%e\n", -9843243.12);
