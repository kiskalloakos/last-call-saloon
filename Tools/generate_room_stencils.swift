// Run from the project root: swift Tools/generate_room_stencils.swift
import AppKit

let output = "Assets/Resources/Art/RoomStencils"
try FileManager.default.createDirectory(atPath: output, withIntermediateDirectories: true)

func image(_ name: String, draw: (CGContext) -> Void) throws {
    let context = CGContext(data: nil, width: 768, height: 512, bitsPerComponent: 8,
                            bytesPerRow: 0, space: CGColorSpaceCreateDeviceRGB(),
                            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
    context.setStrokeColor(CGColor(gray: 1, alpha: 1))
    context.setFillColor(CGColor(gray: 1, alpha: 1))
    context.setLineWidth(15)
    context.setLineCap(.round)
    context.setLineJoin(.round)
    draw(context)
    // Tiny gaps give the flat stencil a light, worn paint finish.
    context.setBlendMode(.clear)
    for i in 0..<450 {
        let x = CGFloat((i * 173 + 41) % 768), y = CGFloat((i * 97 + 23) % 512)
        context.fillEllipse(in: CGRect(x: x, y: y, width: 1.5, height: 1.5))
    }
    let cropped = context.makeImage()!.cropping(to: CGRect(x: 128, y: 24, width: 512, height: 464))!
    let bitmap = NSBitmapImageRep(cgImage: cropped)
    try bitmap.representation(using: .png, properties: [:])!.write(to: URL(fileURLWithPath: "\(output)/\(name).png"))
}

try image("Blackjack") { c in
    func card(_ x: CGFloat, _ y: CGFloat, _ angle: CGFloat, front: Bool) {
        c.saveGState()
        c.translateBy(x: x, y: y); c.rotate(by: angle * .pi / 180)
        let rect = CGRect(x: -104, y: -157, width: 208, height: 314)
        let outline = CGPath(roundedRect: rect, cornerWidth: 18, cornerHeight: 18, transform: nil)
        if front {
            c.setBlendMode(.clear); c.addPath(outline); c.fillPath(); c.setBlendMode(.normal)
        }
        c.addPath(outline); c.strokePath()
        if front {
            // Ace of spades: one clear silhouette, without small lettering.
            c.move(to: CGPoint(x: 0, y: 76))
            c.addCurve(to: CGPoint(x: -4, y: -32), control1: CGPoint(x: -125, y: -17), control2: CGPoint(x: -44, y: -85))
            c.addCurve(to: CGPoint(x: 0, y: 76), control1: CGPoint(x: 80, y: -118), control2: CGPoint(x: 121, y: -16))
            c.fillPath()
            c.move(to: CGPoint(x: 0, y: -15)); c.addLine(to: CGPoint(x: -22, y: -76))
            c.addLine(to: CGPoint(x: 22, y: -76)); c.closePath(); c.fillPath()
        }
        c.restoreGState()
    }
    card(300, 273, 17, front: false)
    card(444, 239, -12, front: true)
}

try image("CoinFlip") { c in
    for (x, y) in [(CGFloat(257), CGFloat(221)), (CGFloat(506), CGFloat(253))] {
        c.strokeEllipse(in: CGRect(x: x-94, y: y-94, width: 188, height: 188))
        c.setLineWidth(6)
        c.strokeEllipse(in: CGRect(x: x-68, y: y-68, width: 136, height: 136))
        c.setLineWidth(15)
    }
    // A simple sun/star on one face; a single bar on the other.
    c.move(to: CGPoint(x: 257, y: 263)); c.addLine(to: CGPoint(x: 269, y: 233))
    c.addLine(to: CGPoint(x: 299, y: 221)); c.addLine(to: CGPoint(x: 269, y: 209))
    c.addLine(to: CGPoint(x: 257, y: 179)); c.addLine(to: CGPoint(x: 245, y: 209))
    c.addLine(to: CGPoint(x: 215, y: 221)); c.addLine(to: CGPoint(x: 245, y: 233))
    c.closePath(); c.fillPath()
    c.move(to: CGPoint(x: 506, y: 217)); c.addLine(to: CGPoint(x: 506, y: 289)); c.strokePath()
    c.move(to: CGPoint(x: 271, y: 362))
    c.addCurve(to: CGPoint(x: 500, y: 391), control1: CGPoint(x: 325, y: 438), control2: CGPoint(x: 442, y: 444))
    c.strokePath()
    c.move(to: CGPoint(x: 452, y: 390)); c.addLine(to: CGPoint(x: 500, y: 391))
    c.addLine(to: CGPoint(x: 489, y: 437)); c.strokePath()
}

try image("BluffDice") { c in
    // A lifted cup and two tilted dice, in the same broad painted strokes.
    c.saveGState()
    c.translateBy(x: 273, y: 337); c.rotate(by: 15 * .pi / 180)
    c.move(to: CGPoint(x: -79, y: 68)); c.addLine(to: CGPoint(x: 79, y: 68))
    c.addLine(to: CGPoint(x: 99, y: -89)); c.addLine(to: CGPoint(x: -99, y: -89))
    c.closePath(); c.strokePath()
    c.move(to: CGPoint(x: -90, y: -67)); c.addLine(to: CGPoint(x: 90, y: -67)); c.strokePath()
    c.restoreGState()
    func die(_ x: CGFloat, _ y: CGFloat, _ angle: CGFloat, face: Int) {
        c.saveGState(); c.translateBy(x: x, y: y); c.rotate(by: angle * .pi / 180)
        let outline = CGPath(roundedRect: CGRect(x: -79, y: -79, width: 158, height: 158), cornerWidth: 15, cornerHeight: 15, transform: nil)
        c.setBlendMode(.clear); c.addPath(outline); c.fillPath(); c.setBlendMode(.normal)
        c.addPath(outline); c.strokePath()
        let pips: [(CGFloat, CGFloat)] = face == 3 ? [(-38,-38),(0,0),(38,38)] : [(-38,-38),(-38,38),(0,0),(38,-38),(38,38)]
        for (px, py) in pips { c.fillEllipse(in: CGRect(x: px-12, y: py-12, width: 24, height: 24)) }
        c.restoreGState()
    }
    die(453, 290, -17, face: 5)
    die(319, 139, 13, face: 3)
}
