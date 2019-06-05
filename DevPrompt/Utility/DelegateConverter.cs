﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace DevPrompt.Utility
{
    internal class DelegateConverter : IValueConverter
    {
        public delegate object ConvertFunc(object value, Type targetType, object parameter);

        private DelegateConverter.ConvertFunc convert;
        private DelegateConverter.ConvertFunc convertBack;

        public DelegateConverter(DelegateConverter.ConvertFunc convert, DelegateConverter.ConvertFunc convertBack = null)
        {
            this.convert = convert;
            this.convertBack = convertBack;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (this.convert != null)
                ? this.convert(value, targetType, parameter)
                : throw new InvalidOperationException();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (this.convertBack != null)
                ? this.convertBack(value, targetType, parameter)
                : throw new InvalidOperationException();
        }
    }
}
