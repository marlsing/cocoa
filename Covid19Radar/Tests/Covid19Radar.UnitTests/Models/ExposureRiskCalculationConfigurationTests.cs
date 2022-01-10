﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.IO;
using Covid19Radar.Model;
using Newtonsoft.Json;
using Xunit;

namespace Covid19Radar.UnitTests.Models
{
    public class ExposureRiskCalculationConfigurationTests
    {

        private const string JSON_V1_EXPOSURE_RISK_CALCULATION_CONFIGURATION = "exposure_risk_calculation_configuration_v1_1.json";

        public ExposureRiskCalculationConfigurationTests()
        {
        }

        private static string GetTestJson(string fileName)
        {
            var path = TestDataUtils.GetLocalFilePath(fileName);
            using (var reader = File.OpenText(path))
            {
                return reader.ReadToEnd();
            }
        }

        [Fact]
        public void JsonSerializeTests()
        {
            var expectedJson = GetTestJson(JSON_V1_EXPOSURE_RISK_CALCULATION_CONFIGURATION);

            V1ExposureRiskCalculationConfiguration configuration = new V1ExposureRiskCalculationConfiguration()
            {
                DailySummary_DaySummary_ScoreSum = new V1ExposureRiskCalculationConfiguration.Threshold() {
                    Op = V1ExposureRiskCalculationConfiguration.Threshold.OPERATION_GREATER_EQUAL,
                    Value = 2000.0
                }
            };
            var serializedJson = JsonConvert.SerializeObject(configuration);

            Assert.NotEmpty(serializedJson);
            Assert.Equal(expectedJson, serializedJson);
        }

        [Fact]
        public void JsonDeserializeTests()
        {
            V1ExposureRiskCalculationConfiguration expected = new V1ExposureRiskCalculationConfiguration()
            {
                DailySummary_DaySummary_ScoreSum = new V1ExposureRiskCalculationConfiguration.Threshold()
                {
                    Op = V1ExposureRiskCalculationConfiguration.Threshold.OPERATION_GREATER_EQUAL,
                    Value = 2000.0
                }
            };

            var testJson = GetTestJson(JSON_V1_EXPOSURE_RISK_CALCULATION_CONFIGURATION);
            var deserialized = JsonConvert.DeserializeObject<V1ExposureRiskCalculationConfiguration>(testJson);

            Assert.NotNull(deserialized);
            Assert.Equal(expected, deserialized);
        }
    }
}
