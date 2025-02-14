using Xunit;
using CxStudio;

namespace CxStudioTests
{
    public class TimeTests
    {
        [Fact]
        public void Constructor_WithSeconds_ShouldSetMilliseconds()
        {
            var time = new Time(1.5);
            Assert.Equal(1500, time.ToSeconds() * 1000);
        }

        [Fact]
        public void Constructor_WithMilliseconds_ShouldSetMilliseconds()
        {
            var time = new Time(1500);
            Assert.Equal(1.5, time.ToSeconds());
        }

        [Fact]
        public void FromTimecode_ValidTimecode_ShouldReturnTime()
        {
            var timebase = new Timebase(30, false);
            var time = Time.FromTimecode("01:02:03;15", timebase);
            Assert.NotNull(time);
            Assert.Equal(3723.5, time?.ToSeconds());
        }

        [Fact]
        public void FromTimecode_InvalidTimecode_ShouldReturnNull()
        {
            var timebase = new Timebase(30, false);
            var time = Time.FromTimecode("invalid_timecode", timebase);
            Assert.Null(time);
        }

        [Fact]
        public void FromTimestamp_ValidTimestamp_ShouldReturnTime()
        {
            var time = Time.FromTimestamp("01:02:03.500");
            Assert.NotNull(time);
            Assert.Equal(3723.5, time?.ToSeconds());
        }

        [Fact]
        public void FromTimestamp_InvalidTimestamp_ShouldReturnNull()
        {
            var time = Time.FromTimestamp("invalid_timestamp");
            Assert.Null(time);
        }

        [Fact]
        public void ToTimecode_ShouldReturnCorrectTimecode()
        {
            var time = new Time(3723500);
            var timebase = new Timebase(30, false);
            var timecode = time.ToTimecode(timebase);
            Assert.Equal("01:02:03;15", timecode);
        }

        [Fact]
        public void ToTimestamp_ShouldReturnCorrectTimestamp()
        {
            var time = new Time(3723500);
            var timestamp = time.ToTimestamp();
            Assert.Equal("01:02:03.500", timestamp);
        }

        [Fact]
        public void Operator_Addition_ShouldReturnCorrectResult()
        {
            var time1 = new Time(1000);
            var time2 = new Time(2000);
            var result = time1 + time2;
            Assert.Equal(3000, result.ToSeconds() * 1000);
        }

        [Fact]
        public void Operator_Subtraction_ShouldReturnCorrectResult()
        {
            var time1 = new Time(3000);
            var time2 = new Time(1000);
            var result = time1 - time2;
            Assert.Equal(2000, result.ToSeconds() * 1000);
        }

        [Fact]
        public void Operator_Multiplication_ShouldReturnCorrectResult()
        {
            var time = new Time(1000);
            var result = time * 2;
            Assert.Equal(2000, result.ToSeconds() * 1000);
        }

        [Fact]
        public void Operator_Division_ShouldReturnCorrectResult()
        {
            var time = new Time(2000);
            var result = time / 2;
            Assert.Equal(1000, result.ToSeconds() * 1000);
        }

        [Fact]
        public void Operator_Equality_ShouldReturnTrueForEqualTimes()
        {
            var time1 = new Time(1000);
            var time2 = new Time(1000);
            Assert.True(time1 == time2);
        }

        [Fact]
        public void Operator_Inequality_ShouldReturnTrueForDifferentTimes()
        {
            var time1 = new Time(1000);
            var time2 = new Time(2000);
            Assert.True(time1 != time2);
        }

        [Fact]
        public void Operator_LessThan_ShouldReturnTrueForSmallerTime()
        {
            var time1 = new Time(1000);
            var time2 = new Time(2000);
            Assert.True(time1 < time2);
        }

        [Fact]
        public void Operator_GreaterThan_ShouldReturnTrueForLargerTime()
        {
            var time1 = new Time(2000);
            var time2 = new Time(1000);
            Assert.True(time1 > time2);
        }

        [Fact]
        public void Operator_LessThanOrEqual_ShouldReturnTrueForSmallerOrEqualTime()
        {
            var time1 = new Time(1000);
            var time2 = new Time(2000);
            var time3 = new Time(1000);
            Assert.True(time1 <= time2);
            Assert.True(time1 <= time3);
        }

        [Fact]
        public void Operator_GreaterThanOrEqual_ShouldReturnTrueForLargerOrEqualTime()
        {
            var time1 = new Time(2000);
            var time2 = new Time(1000);
            var time3 = new Time(2000);
            Assert.True(time1 >= time2);
            Assert.True(time1 >= time3);
        }
    }
}
