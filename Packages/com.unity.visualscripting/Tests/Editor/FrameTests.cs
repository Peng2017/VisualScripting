using System;
using NUnit.Framework;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests
{
    [Category("Tracing")]
    class FrameTests
    {
        static void AssertNodeIds(Debugger.VisualScriptingFrameTrace frame, params int[] ids)
        {
            Assert.That(frame.steps.Count, Is.EqualTo(ids.Length));
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.That(frame.steps[i].nodeId, Is.EqualTo(ids[i]));
            }
        }

        [Test]
        public void TestCircularBufferIsWorking()
        {
           TraceRecorder recorder = new TraceRecorder(maxFrames:2);
            Assert.That(recorder.Get(1), Is.Null);
            recorder.Record(1, 1);
            Assert.That(recorder.Get(1), Is.EqualTo(1));

            Assert.That(recorder.Get(2), Is.Null);
            recorder.Record(2, 2);
            Assert.That(recorder.Get(2), Is.EqualTo(2));

            Assert.That(recorder.Get(3), Is.Null);
            recorder.Record(3, 3);
            Assert.That(recorder.Get(3), Is.EqualTo(3));

            Assert.That(recorder.Get(1), Is.Null);
        }

        [Test]
        public void TestCircularBufferHasRightValues()
        {
           TraceRecorder recorder = new TraceRecorder(maxFrames:10);

            for (int i = 0; i < 20; i++)
            {
                recorder.Record(i, i);
            }
            for (int i = 0; i < 20; i++)
            {
                if(i < 10)
                    Assert.That(recorder.Get(i), Is.Null);
                else
                    Assert.That(recorder.Get(i), Is.EqualTo(i));
            }
        }

        [Test]
        public void TestFrame()
        {
            Debugger.VisualScriptingFrameTrace frame = new Debugger.VisualScriptingFrameTrace();

            // _ _ 3
            frame.PadAndInsert(stepOffset: 2, nodeId: 3);
            AssertNodeIds(frame, -1, -1, 3);

            // 1 _ 3
            frame.SetNextStep(1);
            AssertNodeIds(frame, 1, -1, 3);

            // 1 2 3
            frame.SetNextStep(2);
            AssertNodeIds(frame, 1, 2, 3);

            // 1 2 3 4
            frame.SetNextStep(4);
            AssertNodeIds(frame, 1, 2, 3, 4);

            // 1 2 3 4 _ _ 7
            frame.PadAndInsert(2, 7);
            AssertNodeIds(frame, 1, 2, 3, 4, -1, -1, 7);

            // 1 2 3 4 5 _ 7
            frame.SetNextStep(5);
            AssertNodeIds(frame, 1, 2, 3, 4, 5, -1, 7);

            // 1 2 3 4 5 6 7
            frame.SetNextStep(6);
            AssertNodeIds(frame, 1, 2, 3, 4, 5, 6, 7);
        }
    }
}
