// 
// Copyright (c) 2004-2020 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

#if !NETSTANDARD

namespace NLog.LayoutRenderers
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using NLog.Internal;

    /// <summary>
    /// High precision timer, based on the value returned from QueryPerformanceCounter() optionally converted to seconds.
    /// </summary>
    [LayoutRenderer("qpc")]
    public class QueryPerformanceCounterLayoutRenderer : LayoutRenderer
    {
        private bool raw;
        private ulong firstQpcValue;
        private ulong lastQpcValue;
        private double frequency = 1;

        /// <summary>
        /// Gets or sets a value indicating whether to normalize the result by subtracting 
        /// it from the result of the first call (so that it's effectively zero-based).
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultValue(true)]
        public bool Normalize { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to output the difference between the result 
        /// of QueryPerformanceCounter and the previous one.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultValue(false)]
        public bool Difference { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to convert the result to seconds by dividing 
        /// by the result of QueryPerformanceFrequency().
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultValue(true)]
        public bool Seconds
        {
            get => !raw;
            set => raw = !value;
        }

        /// <summary>
        /// Gets or sets the number of decimal digits to be included in output.
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultValue(4)]
        public int Precision { get; set; } = 4;

        /// <summary>
        /// Gets or sets a value indicating whether to align decimal point (emit non-significant zeros).
        /// </summary>
        /// <docgen category='Rendering Options' order='10' />
        [DefaultValue(true)]
        public bool AlignDecimalPoint { get; set; } = true;

        /// <inheritdoc />
        protected override void InitializeLayoutRenderer()
        {
            base.InitializeLayoutRenderer();

            ulong performanceFrequency;

            if (!NativeMethods.QueryPerformanceFrequency(out performanceFrequency))
            {
                throw new InvalidOperationException("Cannot determine high-performance counter frequency.");
            }

            ulong qpcValue;

            if (!NativeMethods.QueryPerformanceCounter(out qpcValue))
            {
                throw new InvalidOperationException("Cannot determine high-performance counter value.");
            }

            frequency = performanceFrequency;
            firstQpcValue = qpcValue;
            lastQpcValue = qpcValue;
        }

        /// <inheritdoc />
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var qpcValue = GetValue();

            if (qpcValue != null)
            {
                string stringValue;

                if (Seconds)
                {
                    var val = ToSeconds(qpcValue.Value);

                    stringValue = Convert.ToString(val, CultureInfo.InvariantCulture);
                    if (AlignDecimalPoint)
                    {
                        int p = stringValue.IndexOf('.');
                        if (p == -1)
                        {
                            stringValue += "." + new string('0', Precision);
                        }
                        else
                        {
                            stringValue += new string('0', Precision - (stringValue.Length - 1 - p));
                        }
                    }
                }
                else
                {
                    stringValue = Convert.ToString(qpcValue, CultureInfo.InvariantCulture);
                }

                builder.Append(stringValue);
            }
        }

        private double ToSeconds(ulong qpcValue)
        {
            double val = Math.Round(qpcValue / frequency, Precision);
            return val;
        }

        private ulong? GetValue()
        {
            if (!NativeMethods.QueryPerformanceCounter(out var qpcValue))
            {
                return null;
            }

            ulong v = qpcValue;

            if (Difference)
            {
                qpcValue -= lastQpcValue;
            }
            else if (Normalize)
            {
                qpcValue -= firstQpcValue;
            }

            lastQpcValue = v;
            return qpcValue;
        }

  
    }
}

#endif