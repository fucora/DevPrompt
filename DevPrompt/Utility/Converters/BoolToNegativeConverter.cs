﻿using DevPrompt.ProcessWorkspace.Utility;
using System;

namespace DevPrompt.Utility.Converters
{
    internal sealed class BoolToNegativeConverter : DelegateConverter
    {
        public BoolToNegativeConverter()
            : base(BoolToNegativeConverter.Convert)
        {
        }

        public static object Convert(object value, Type targetType, object parameter)
        {
            if (value is bool b)
            {
                return !b;
            }

            throw new InvalidOperationException();
        }
    }
}
