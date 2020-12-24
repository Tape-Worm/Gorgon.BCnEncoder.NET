using System;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Gorgon.Graphics;

namespace BCnEncoder.Shared
{
	internal static class PcaVectors
	{
		private const int C565_5_mask = 0xF8;
		private const int C565_6_mask = 0xFC;

		private static void ConvertToVector4(ReadOnlySpan<GorgonColor> colors, Span<Vector4> vectors)
		{
			for (int i = 0; i < colors.Length; i++)
			{
				GorgonColor color = colors[i];
				vectors[i].X += color.Red;
				vectors[i].Y += color.Green;
				vectors[i].Z += color.Blue;
				vectors[i].W += color.Alpha;
			}
		}

		private static Vector4 CalculateMean(Span<Vector4> colors)
		{

			float r = 0;
			float g = 0;
			float b = 0;
			float a = 0;

			for (int i = 0; i < colors.Length; i++)
			{
				r += colors[i].X;
				g += colors[i].Y;
				b += colors[i].Z;
				a += colors[i].W;
			}

			return new Vector4(
				r / colors.Length,
				g / colors.Length,
				b / colors.Length,
				a / colors.Length
				);
		}

		internal static Matrix4x4 CalculateCovariance(Span<Vector4> values, out Vector4 mean) {
			mean = CalculateMean(values);
			for (int i = 0; i < values.Length; i++)
			{
				values[i] -= mean;
			}

			//4x4 matrix
			var mat = new Matrix4x4();

			for (int i = 0; i < values.Length; i++)
			{
				mat.M11 += values[i].X * values[i].X;
				mat.M12 += values[i].X * values[i].Y;
				mat.M13 += values[i].X * values[i].Z;
				mat.M14 += values[i].X * values[i].W;
				
				mat.M22 += values[i].Y * values[i].Y;
				mat.M23 += values[i].Y * values[i].Z;
				mat.M24 += values[i].Y * values[i].W;
				
				mat.M33 += values[i].Z * values[i].Z;
				mat.M34 += values[i].Z * values[i].W;
				
				mat.M44 += values[i].W * values[i].W;
			}

			mat = Matrix4x4.Multiply(mat, 1f / (values.Length - 1));
			
			mat.M21 = mat.M12;
			mat.M31 = mat.M13;
			mat.M32 = mat.M23;
			mat.M41 = mat.M14;
			mat.M42 = mat.M24;
			mat.M43 = mat.M34;

			return mat;
		}

		/// <summary>
		/// Calculate principal axis with the power-method
		/// </summary>
		/// <param name="covarianceMatrix"></param>
		/// <returns></returns>
		internal static Vector4 CalculatePrincipalAxis(Matrix4x4 covarianceMatrix) {
			Vector4 lastDa = Vector4.UnitY;

			for (int i = 0; i < 30; i++) {
				var dA = Vector4.Transform(lastDa, covarianceMatrix);

				if(dA.LengthSquared() == 0) {
					break;
				}

				dA = Vector4.Normalize(dA);
				if (Vector4.Dot(lastDa, dA) > 0.999999) {
					lastDa = dA;
					break;
				}
				else {
					lastDa = dA;
				}
			}
			return lastDa;
		}

		public static void Create(Span<GorgonColor> colors, out Vector3 mean, out Vector3 principalAxis)
		{
			Span<Vector4> vectors = stackalloc Vector4[colors.Length];
			ConvertToVector4(colors, vectors);


            Matrix4x4 cov = CalculateCovariance(vectors, out Vector4 v4Mean);
			mean = new Vector3(v4Mean.X, v4Mean.Y, v4Mean.Z);

            Vector4 pa = CalculatePrincipalAxis(cov);
			principalAxis = new Vector3(pa.X, pa.Y, pa.Z);
			if (principalAxis.LengthSquared() == 0) {
				principalAxis = Vector3.UnitY;
			}
			else {
				principalAxis = Vector3.Normalize(principalAxis);
			}

		}

		public static void CreateWithAlpha(Span<GorgonColor> colors, out Vector4 mean, out Vector4 principalAxis)
		{
			Span<Vector4> vectors = stackalloc Vector4[colors.Length];
			ConvertToVector4(colors, vectors);

            Matrix4x4 cov = CalculateCovariance(vectors, out mean);
			principalAxis = CalculatePrincipalAxis(cov);
		}


		public static void GetExtremePoints(Span<int> colors, Vector3 mean, Vector3 principalAxis, out ColorRgb24 min,
			out ColorRgb24 max)
		{

			float minD = 0;
			float maxD = 0;

			for (int i = 0; i < colors.Length; i++)
			{
				var colorVec = GorgonColor.FromRGBA(colors[i]).ToVector3();

                Vector3 v = colorVec - mean;
                float d = Vector3.Dot(v, principalAxis);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			Vector3 minVec = mean + (principalAxis * minD);
			Vector3 maxVec = mean + (principalAxis * maxD);

			int minR = (int) (minVec.X * 255);
			int minG = (int) (minVec.Y * 255);
			int minB = (int) (minVec.Z * 255);

			int maxR = (int) (maxVec.X * 255);
			int maxG = (int) (maxVec.Y * 255);
			int maxB = (int) (maxVec.Z * 255);

			minR = (minR >= 0) ? minR : 0;
			minG = (minG >= 0) ? minG : 0;
			minB = (minB >= 0) ? minB : 0;

			maxR = (maxR <= 255) ? maxR : 255;
			maxG = (maxG <= 255) ? maxG : 255;
			maxB = (maxB <= 255) ? maxB : 255;

			min = new ColorRgb24((byte)minR, (byte)minG, (byte)minB);
			max = new ColorRgb24((byte)maxR, (byte)maxG, (byte)maxB);
		}

		public static void GetMinMaxColor565(Span<GorgonColor> colors, Vector3 mean, Vector3 principalAxis, 
			out ColorRgb565 min, out ColorRgb565 max)
		{

			float minD = 0;
			float maxD = 0;

			for (int i = 0; i < colors.Length; i++)
			{
				var colorVec = (Vector3)colors[i];

                Vector3 v = colorVec - mean;
                float d = Vector3.Dot(v, principalAxis);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			//Inset
			minD *= 15 / 16f;
			maxD *= 15 / 16f;

			Vector3 minVec = mean + (principalAxis * minD);
			Vector3 maxVec = mean + (principalAxis * maxD);

			int minR = (int) (minVec.X * 255);
			int minG = (int) (minVec.Y * 255);
			int minB = (int) (minVec.Z * 255);

			int maxR = (int) (maxVec.X * 255);
			int maxG = (int) (maxVec.Y * 255);
			int maxB = (int) (maxVec.Z * 255);

			minR = (minR >= 0) ? minR : 0;
			minG = (minG >= 0) ? minG : 0;
			minB = (minB >= 0) ? minB : 0;

			maxR = (maxR <= 255) ? maxR : 255;
			maxG = (maxG <= 255) ? maxG : 255;
			maxB = (maxB <= 255) ? maxB : 255;

			// Optimal round
			minR = (minR & C565_5_mask) | (minR >> 5);
			minG = (minG & C565_6_mask) | (minG >> 6);
			minB = (minB & C565_5_mask) | (minB >> 5);

			maxR = (maxR & C565_5_mask) | (maxR >> 5);
			maxG = (maxG & C565_6_mask) | (maxG >> 6);
			maxB = (maxB & C565_5_mask) | (maxB >> 5);

			min = new ColorRgb565((byte)minR, (byte)minG, (byte)minB);
			max = new ColorRgb565((byte)maxR, (byte)maxG, (byte)maxB);

		}

		public static void GetExtremePointsWithAlpha(Span<GorgonColor> colors, Vector4 mean, Vector4 principalAxis, out Vector4 min,
			out Vector4 max)
		{

			float minD = 0;
			float maxD = 0;

			for (int i = 0; i < colors.Length; i++)
			{				
				Vector4 colorVec = colors[i];
				Vector4 v = colorVec - mean;
                float d = Vector4.Dot(v, principalAxis);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			min = mean + (principalAxis * minD);
			max = mean + (principalAxis * maxD);
		}

		public static void GetOptimizedEndpoints565(Span<int> colors, Vector3 mean, Vector3 principalAxis, out ColorRgb565 min, out ColorRgb565 max,
			float rWeight = 0.3f, float gWeight = 0.6f, float bWeight = 0.1f)
		{
			int length = colors.Length;
			var vectorColors = new Vector3[length];
			for (int i = 0; i < colors.Length; i++)
			{
				vectorColors[i] = GorgonColor.FromRGBA(colors[i]).ToVector3();
			}

			float minD = 0;
			float maxD = 0;

			Vector3 Clamp565(Vector3 vec)
			{
				if (vec.X < 0)
                {
                    vec.X = 0;
                }

                if (vec.X > 31)
                {
                    vec.X = 31;
                }

                if (vec.Y < 0)
                {
                    vec.Y = 0;
                }

                if (vec.Y > 63)
                {
                    vec.Y = 63;
                }

                if (vec.Z < 0)
                {
                    vec.Z = 0;
                }

                if (vec.Z > 31)
                {
                    vec.Z = 31;
                }

                return new Vector3((float)Math.Round(vec.X), (float)Math.Round(vec.Y), (float)Math.Round(vec.Z));
			}

			float Distance(Vector3 v, Vector3 p)
			{
				return (v.X - p.X) * (v.X - p.X) * rWeight
				       + (v.Y - p.Y) * (v.Y - p.Y) * gWeight
				       + (v.Z - p.Z) * (v.Z - p.Z) * bWeight;
				;
			}

			float SelectClosestDistance(Vector3 selector, Vector3 f0, Vector3 f1, Vector3 f2, Vector3 f3)
			{
				float d0 = Distance(selector, f0);
				float d1 = Distance(selector, f1);
				float d2 = Distance(selector, f2);
				float d3 = Distance(selector, f3);

                return d0 < d1 && d0 < d2 && d0 < d3 ? d0 : d1 < d0 && d1 < d2 && d1 < d3 ? d1 : d2 < d0 && d2 < d1 && d2 < d3 ? d2 : d3;
            }

            Vector3 endPoint0;
			Vector3 endPoint1;

			double CalculateError()
			{
				double cumulativeError = 0;
				var ep0 = new Vector3(endPoint0.X / 31, endPoint0.Y / 63, endPoint0.Z / 31);
				var ep1 = new Vector3(endPoint1.X / 31, endPoint1.Y / 63, endPoint1.Z / 31);
				Vector3 ep2 = ep0 + ((ep1 - ep0) * 1 / 3f);
				Vector3 ep3 = ep0 + ((ep1 - ep0) * 2 / 3f);

				for (int i = 0; i < length; i++)
				{
					double distance = SelectClosestDistance(vectorColors[i], ep0, ep1, ep2, ep3);
					cumulativeError += distance;
				}
				return cumulativeError;
			}


			for (int i = 0; i < vectorColors.Length; i++)
			{
				float d = ProjectPointOnLine(vectorColors[i], mean, principalAxis);
				if (d < minD)
                {
                    minD = d;
                }

                if (d > maxD)
                {
                    maxD = d;
                }
            }

			endPoint0 = mean + (principalAxis * minD);
			endPoint1 = mean + (principalAxis * maxD);

			endPoint0 = new Vector3((float)Math.Round(endPoint0.X * 31), (float)Math.Round(endPoint0.Y * 63), (float)Math.Round(endPoint0.Z * 31));
			endPoint1 = new Vector3((float)Math.Round(endPoint1.X * 31), (float)Math.Round(endPoint1.Y * 63), (float)Math.Round(endPoint1.Z * 31));
			endPoint0 = Clamp565(endPoint0);
			endPoint1 = Clamp565(endPoint1);

			double best = CalculateError();
			int increment = 5;
			bool foundBetter = true;
			int rounds = 0;
			// Variate color and look for better endpoints
			while (increment > 1 || foundBetter)
			{
				rounds++;
				foundBetter = false;
				{ // decrement ep0
                    Vector3 prev = endPoint0;
					endPoint0 -= principalAxis * increment * 2;
					endPoint0 = Clamp565(endPoint0);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev;
					}
				}

				{ // decrement ep1
                    Vector3 prev = endPoint1;
					endPoint1 -= principalAxis * increment * 2;
					endPoint1 = Clamp565(endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1 = prev;
					}
				}

				{ // increment ep0
                    Vector3 prev = endPoint0;
					endPoint0 += principalAxis * increment * 2;
					endPoint0 = Clamp565(endPoint0);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev;
					}
				}

				{ // increment ep1
                    Vector3 prev = endPoint1;
					endPoint1 += principalAxis * increment * 2;
					endPoint1 = Clamp565(endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1 = prev;
					}
				}

				{ // scaleUp 
                    Vector3 prev0 = endPoint0;
                    Vector3 prev1 = endPoint1;
					endPoint0 -= principalAxis * increment * 2;
					endPoint1 += principalAxis * increment * 2;
					endPoint0 = Clamp565(endPoint0);
					endPoint1 = Clamp565(endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev0;
						endPoint1 = prev1;
					}
				}

				{ // scaleDown
                    Vector3 prev0 = endPoint0;
                    Vector3 prev1 = endPoint1;
					endPoint0 += principalAxis * increment * 2;
					endPoint1 -= principalAxis * increment * 2;
					endPoint0 = Clamp565(endPoint0);
					endPoint1 = Clamp565(endPoint1);
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0 = prev0;
						endPoint1 = prev1;
					}
				}

				#region G
				if (endPoint0.Y - increment >= 0)
				{ // decrement ep0 G
					float prevY = endPoint0.Y;
					endPoint0.Y -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Y = prevY;
					}
				}

				if (endPoint1.Y - increment >= 0)
				{ // decrement ep1 G
					float prevY = endPoint1.Y;
					endPoint1.Y -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Y = prevY;
					}
				}

				if (foundBetter && increment > 1)
				{
					increment--;
				}

				if (endPoint1.Y + increment <= 63)
				{ // increment ep1 G
					float prevY = endPoint1.Y;
					endPoint1.Y += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Y = prevY;
					}
				}

				if (endPoint0.Y + increment <= 63)
				{ // increment ep0 G
					float prevY = endPoint0.Y;
					endPoint0.Y += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Y = prevY;
					}
				}

				#endregion

				#region R
				if (endPoint0.X - increment >= 0)
				{ // decrement ep0 R
					float prevX = endPoint0.X;
					endPoint0.X -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.X = prevX;
					}
				}

				if (endPoint1.X - increment >= 0)
				{ // decrement ep1 R
					float prevX = endPoint1.X;
					endPoint1.X -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.X = prevX;
					}
				}

				if (foundBetter && increment > 1)
				{
					increment--;
				}

				if (endPoint1.X + increment <= 31)
				{ // increment ep1 R
					float prevX = endPoint1.X;
					endPoint1.X += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.X = prevX;
					}
				}

				if (endPoint0.X + increment <= 31)
				{ // increment ep0 R
					float prevX = endPoint0.X;
					endPoint0.X += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.X = prevX;
					}
				}
				#endregion

				#region B

				if (endPoint0.Z - increment >= 0)
				{ // decrement ep0 B
					float prevZ = endPoint0.Z;
					endPoint0.Z -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Z = prevZ;
					}
				}

				if (endPoint1.Z - increment >= 0)
				{ // decrement ep1 B
					float prevZ = endPoint1.Z;
					endPoint1.Z -= increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Z = prevZ;
					}
				}

				if (foundBetter && increment > 1)
				{
					increment--;
				}

				if (endPoint1.Z + increment <= 31)
				{ // increment ep1 B
					float prevZ = endPoint1.Z;
					endPoint1.Z += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint1.Z = prevZ;
					}
				}

				if (endPoint0.Z + increment <= 31)
				{ // increment ep0 B
					float prevZ = endPoint0.Z;
					endPoint0.Z += increment;
					double error = CalculateError();
					if (error < best)
					{
						foundBetter = true;
						best = error;
					}
					else
					{
						endPoint0.Z = prevZ;
					}
				}

				#endregion

				endPoint0 = Clamp565(endPoint0);
				endPoint1 = Clamp565(endPoint1);

				if (!foundBetter && increment > 1)
				{
					increment--;
				}
			}

            min = new ColorRgb565
            {
                RawR = (int)endPoint0.X,
                RawG = (int)endPoint0.Y,
                RawB = (int)endPoint0.Z
            };

            max = new ColorRgb565
            {
                RawR = (int)endPoint1.X,
                RawG = (int)endPoint1.Y,
                RawB = (int)endPoint1.Z
            };
        }

		private static float ProjectPointOnLine(Vector3 point, Vector3 linePoint, Vector3 lineDir)
		{
            Vector3 v = point - linePoint;
            float d = Vector3.Dot(v, lineDir);
			return d;
		}
	}
}
