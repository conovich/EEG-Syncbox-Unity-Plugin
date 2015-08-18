#!/bin/sh
#install_name_tool -id @loader_path/liblabjackusb-2.5.2.dylib liblabjackusb-2.5.2.dylib
#install_name_tool -change /usr/local/lib/libusb-1.0.0.dylib @loader_path/libusb-1.0.0.dylib liblabjackusb-2.5.2.dylib
#echo OTOOL BEGIN
#otool -L liblabjackusb-2.5.2.dylib
#echo OTOOL END
#install_name_tool -id @loader_path/libusb-1.0.0.dylib libusb-1.0.0.dylib
#echo OTOOL BEGIN
#otool -L libusb-1.0.0.dylib
#echo OTOOL END
