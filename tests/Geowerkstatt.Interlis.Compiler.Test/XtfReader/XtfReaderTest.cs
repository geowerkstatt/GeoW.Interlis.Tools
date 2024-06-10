using Geowerkstatt.Interlis.Tools.NTS;

namespace Geowerkstatt.Interlis.Tools.XtfReader;

[TestClass]
public class XtfReaderTest
{
    [TestMethod]
    public void ReadXtf()
    {
        var input = """
        <?xml version="1.0" encoding="UTF-8"?><ili:transfer xmlns:ili="http://www.interlis.ch/xtf/2.4/INTERLIS" xmlns:geom="http://www.interlis.ch/geometry/1.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:ModelA="http://www.interlis.ch/xtf/2.4/ModelA">
        <ili:headersection><ili:models><ili:model>ModelA</ili:model></ili:models><ili:sender>ili2gpkg-5.0.1-447c03f22b8f346a44ada86c553d10110872fd40</ili:sender></ili:headersection>
        <ili:datasection>
        <ModelA:TopicA ili:bid="_a1194f35-8028-4249-91bc-211da300a8e2">
        <ModelA:ClassB ili:tid="_3e04a1bb-937b-4979-84be-074d03269bdf"><ModelA:Number>1</ModelA:Number><ModelA:DateA>2024-06-10</ModelA:DateA></ModelA:ClassB>
        <ModelA:ClassB ili:tid="_03de9f36-452d-4777-b80d-fcf4090e7971"><ModelA:Number>3</ModelA:Number><ModelA:DateA>2024-06-12</ModelA:DateA></ModelA:ClassB>
        <ModelA:ClassB ili:tid="_308d1444-dc56-4be3-82df-a2fa82f4c2bb"></ModelA:ClassB>
        <ModelA:ClassA ili:tid="_70771a35-4cd0-4454-927a-3523ec00d62d"><ModelA:Geometry><geom:surface><geom:exterior><geom:polyline><geom:coord><geom:c1>2757410.466</geom:c1><geom:c2>1206884.757</geom:c2></geom:coord><geom:coord><geom:c1>2757419.420</geom:c1><geom:c2>1206818.012</geom:c2></geom:coord><geom:coord><geom:c1>2757473.955</geom:c1><geom:c2>1206844.873</geom:c2></geom:coord><geom:coord><geom:c1>2757483.723</geom:c1><geom:c2>1206885.571</geom:c2></geom:coord><geom:coord><geom:c1>2757410.466</geom:c1><geom:c2>1206884.757</geom:c2></geom:coord></geom:polyline></geom:exterior></geom:surface></ModelA:Geometry><ModelA:Type>Type1</ModelA:Type><ModelA:CBref ili:ref="_03de9f36-452d-4777-b80d-fcf4090e7971"></ModelA:CBref></ModelA:ClassA>
        <ModelA:ClassA ili:tid="_5be9e257-75f5-4ccb-a79b-9bf1ea5bd343"><ModelA:Geometry><geom:surface><geom:exterior><geom:polyline><geom:coord><geom:c1>2757516.281</geom:c1><geom:c2>1206759.407</geom:c2></geom:coord><geom:coord><geom:c1>2757552.091</geom:c1><geom:c2>1206797.555</geom:c2></geom:coord><geom:coord><geom:c1>2757606.631</geom:c1><geom:c2>1206784.640</geom:c2></geom:coord><geom:coord><geom:c1>2757608.057</geom:c1><geom:c2>1206734.927</geom:c2></geom:coord><geom:coord><geom:c1>2757585.404</geom:c1><geom:c2>1206680.293</geom:c2></geom:coord><geom:coord><geom:c1>2757506.514</geom:c1><geom:c2>1206715.454</geom:c2></geom:coord><geom:coord><geom:c1>2757502.444</geom:c1><geom:c2>1206746.384</geom:c2></geom:coord><geom:coord><geom:c1>2757516.281</geom:c1><geom:c2>1206759.407</geom:c2></geom:coord></geom:polyline></geom:exterior></geom:surface></ModelA:Geometry><ModelA:Type>Type2</ModelA:Type><ModelA:CBref ili:ref="_308d1444-dc56-4be3-82df-a2fa82f4c2bb"></ModelA:CBref></ModelA:ClassA>
        <ModelA:ClassA ili:tid="_f1e726bf-b8fe-417f-8310-daa4e16751f0"><ModelA:Geometry><geom:surface><geom:exterior><geom:polyline><geom:coord><geom:c1>2757697.794</geom:c1><geom:c2>1206669.872</geom:c2></geom:coord><geom:coord><geom:c1>2757679.887</geom:c1><geom:c2>1206643.825</geom:c2></geom:coord><geom:coord><geom:c1>2757718.143</geom:c1><geom:c2>1206618.592</geom:c2></geom:coord><geom:coord><geom:c1>2757736.864</geom:c1><geom:c2>1206654.407</geom:c2></geom:coord><geom:coord><geom:c1>2757697.794</geom:c1><geom:c2>1206669.872</geom:c2></geom:coord></geom:polyline></geom:exterior></geom:surface></ModelA:Geometry><ModelA:Type>Type3</ModelA:Type><ModelA:CBref ili:ref="_3e04a1bb-937b-4979-84be-074d03269bdf"></ModelA:CBref></ModelA:ClassA>
        </ModelA:TopicA>
        </ili:datasection>
        </ili:transfer>
        """;

        var reader = new XtfReader();
        var obj = reader
            .ReadXtf(new StringReader(input))
            .Select(o => o.Tid)
            .ToList();

        Assert.AreEqual(6, obj.Count);
    }

    [TestMethod]
    public void ReadXtfWithArc()
    {
        var input = """
        <?xml version="1.0" encoding="UTF-8"?>
        <ili:transfer xmlns:ili="http://www.interlis.ch/xtf/2.4/INTERLIS"
            xmlns:geom="http://www.interlis.ch/geometry/1.0"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
            xmlns:ModelA="http://www.interlis.ch/xtf/2.4/ModelA">
            <ili:headersection>
                <ili:models>
                    <ili:model>ModelA</ili:model>
                </ili:models>
                <ili:sender>ili2gpkg-5.0.1-447c03f22b8f346a44ada86c553d10110872fd40</ili:sender>
            </ili:headersection>
            <ili:datasection>
                <ModelA:TopicA ili:bid="_a1194f35-8028-4249-91bc-211da300a8e2">
                    <ModelA:ClassA ili:tid="_5be9e257-75f5-4ccb-a79b-9bf1ea5bd343">
                        <ModelA:Geometry>
                            <geom:surface>
                                <geom:exterior>
                                    <geom:polyline>
                                        <geom:coord>
                                            <geom:c1>0</geom:c1>
                                            <geom:c2>0</geom:c2>
                                        </geom:coord>
                                        <geom:coord>
                                            <geom:c1>0</geom:c1>
                                            <geom:c2>-100</geom:c2>
                                        </geom:coord>
                                        <geom:arc>
                                            <geom:c1>0</geom:c1>
                                            <geom:c2>100</geom:c2>
                                            <geom:a1>100</geom:a1>
                                            <geom:a2>0</geom:a2>
                                        </geom:arc>
                                        <geom:coord>
                                            <geom:c1>0</geom:c1>
                                            <geom:c2>0</geom:c2>
                                        </geom:coord>
                                    </geom:polyline>
                                </geom:exterior>
                            </geom:surface>
                        </ModelA:Geometry>
                    </ModelA:ClassA>
                </ModelA:TopicA>
            </ili:datasection>
        </ili:transfer>
        """;

        var reader = new XtfReader();
        var obj = reader
            .ReadXtf(new StringReader(input))
            .Select(o => ((CurvePolygon)o.Attributes.Values.Single()).ConvertToPolygon(0.1))
            .ToList();

        Assert.AreEqual(1, obj.Count);
        var polygon = obj[0];
        Assert.IsTrue(polygon.IsValid);
        Assert.AreEqual(39, polygon.Boundary.Coordinates.Length);

    }
}
