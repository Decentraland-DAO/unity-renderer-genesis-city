using System;
using System.Collections;
using System.Linq;
using DCL.Helpers;
using DCL.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using Environment = DCL.Environment;

namespace CullingControllerTests
{
    public class CullingControllerAndScenesShould : IntegrationTestSuite_Legacy
    {
        protected override IEnumerator SetUp()
        {
            yield return base.SetUp();

            Assert.IsFalse(Environment.i.platform.cullingController.IsRunning());

            // If we get the settings copy here, it can be overriden with unwanted values
            // by QualitySettingsController, breaking this test!!. 
            var settings = new CullingControllerSettings();
            settings.maxTimeBudget = 99999;
            Environment.i.platform.cullingController.SetSettings(settings);
            Environment.i.platform.cullingController.Start();

            Assert.IsTrue(Environment.i.platform.cullingController.IsRunning());
            Assert.IsTrue(settings.enableObjectCulling);
        }

        [UnityTest]
        public IEnumerator CullMovingEntities()
        {
            var boxShape = TestHelpers.CreateEntityWithBoxShape(scene, Vector3.one * 1000, true);
            var entity = boxShape.attachedEntities.First();

            Assert.IsTrue(Environment.i.platform.cullingController.IsDirty(), "culling controller not dirty");
            Assert.IsTrue(Environment.i.platform.cullingController.objectsTracker.IsDirty(), "object tracker not dirty");

            yield return boxShape.routine;

            yield return
                new DCL.WaitUntil(() => entity.meshesInfo.renderers[0].forceRenderingOff, 0.3f);

            Assert.IsTrue(entity.meshesInfo.renderers[0].forceRenderingOff, "renderer wasn't hidden!");

            TestHelpers.SetEntityTransform(scene, entity, Vector3.zero, Quaternion.identity, Vector3.one);

            yield return
                new DCL.WaitUntil(() => !entity.meshesInfo.renderers[0].forceRenderingOff, 0.3f);

            Assert.IsFalse(entity.meshesInfo.renderers[0].forceRenderingOff, "renderer wasn't brought back!");
        }

        [UnityTest]
        public IEnumerator TogglingOnAndOffSetRenderersCorrectly()
        {
            var boxShape = TestHelpers.CreateEntityWithBoxShape(scene, Vector3.one * 1000, true);
            var entity = boxShape.attachedEntities.First();

            Assert.IsTrue(Environment.i.platform.cullingController.IsDirty(), "culling controller not dirty");
            Assert.IsTrue(Environment.i.platform.cullingController.objectsTracker.IsDirty(), "object tracker not dirty");

            yield return boxShape.routine;

            yield return
                new DCL.WaitUntil(() => entity.meshesInfo.renderers[0].forceRenderingOff, 0.3f);

            Assert.IsTrue(entity.meshesInfo.renderers[0].forceRenderingOff, "renderer wasn't hidden!");

            TestHelpers.SetEntityTransform(scene, entity, Vector3.zero, Quaternion.identity, Vector3.one);

            var settings = Environment.i.platform.cullingController.GetSettingsCopy();
            settings.enableObjectCulling = false;
            Environment.i.platform.cullingController.SetSettings(settings);

            yield return
                new DCL.WaitUntil(() => !entity.meshesInfo.renderers[0].forceRenderingOff, 0.3f);

            Assert.IsFalse(entity.meshesInfo.renderers[0].forceRenderingOff, "renderer wasn't brought back!");
        }

        [UnityTest]
        public IEnumerator StartStopCullingWithDisabledObjectsCorrectly()
        {
            var boxShape = TestHelpers.CreateEntityWithBoxShape(scene, Vector3.one * 1000, true);
            var entity = boxShape.attachedEntities.First();

            Assert.IsTrue(Environment.i.platform.cullingController.IsDirty(), "culling controller not dirty");
            Assert.IsTrue(Environment.i.platform.cullingController.objectsTracker.IsDirty(), "object tracker not dirty");

            yield return boxShape.routine;

            yield return
                new DCL.WaitUntil(() => entity.meshesInfo.renderers[0].forceRenderingOff, 0.3f);

            Assert.IsTrue(entity.meshesInfo.renderers[0].forceRenderingOff, "renderer wasn't hidden!");

            entity.gameObject.SetActive(false);
            yield return null;

            Environment.i.platform.cullingController.Stop();
            TestHelpers.SetEntityTransform(scene, entity, Vector3.zero, Quaternion.identity, Vector3.one);

            yield return
                new DCL.WaitUntil(() => !entity.meshesInfo.renderers[0].forceRenderingOff, 0.3f);

            Assert.IsFalse(entity.meshesInfo.renderers[0].forceRenderingOff, "renderer wasn't brought back!");
        }
    }
}